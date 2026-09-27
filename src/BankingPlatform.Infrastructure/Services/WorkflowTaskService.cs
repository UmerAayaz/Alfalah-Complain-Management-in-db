using System.Text.Json;
using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace BankingPlatform.Infrastructure.Services;

public sealed class WorkflowTaskService(
    AppDbContext db,
    ICurrentUser currentUser,
    IWorkflowRuntime runtime,
    IFileStorage fileStorage) : IWorkflowTaskService
{
    private readonly AppDbContext _db = db;
    private readonly IFileStorage _fileStorage = fileStorage;

    // ============================================================
    // MY BUCKET
    // ============================================================

    public async Task<IReadOnlyList<WorkflowTaskDto>> GetMyBucketAsync(CancellationToken cancellationToken)
    {
        var memberships = await db.DepartmentMemberships.AsNoTracking()
            .Where(x => x.UserId == currentUser.UserId)
            .Select(x => new { x.DepartmentId, x.RoleCode })
            .ToListAsync(cancellationToken);

        var roleKeys = memberships
            .Select(x => x.DepartmentId + "|" + x.RoleCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tasks = await db.WorkflowTasks.AsNoTracking()
            .Include(x => x.Complaint)
            .Include(x => x.WorkflowNode)
            .Include(x => x.AssignedToUser)
            .Where(x => x.Status == WorkflowTaskStatus.Open &&
                        (x.AssignedToUserId == currentUser.UserId || x.AssignedToUserId == null))
            .OrderBy(x => x.DueAtUtc ?? DateTime.MaxValue)
            .ToListAsync(cancellationToken);

        return tasks
            .Where(x => x.AssignedToUserId == currentUser.UserId ||
                        (x.AssignedToUserId is null &&
                         x.AssignedRoleCode is not null &&
                         roleKeys.Contains(x.DepartmentId + "|" + x.AssignedRoleCode)))
            .Select(x => new WorkflowTaskDto(
                x.Id,
                x.ComplaintId,
                x.DepartmentId,
                x.Complaint.ComplaintNumber,
                x.Complaint.Subject,
                x.WorkflowNode.Name,
                x.AssignedRoleCode,
                x.AssignedToUserId,
                x.AssignedToUser?.DisplayName,
                x.OpenedAtUtc,
                x.DueAtUtc,
                x.EscalatedAtUtc))
            .ToList();
    }

    // ============================================================
    // ACTIONS
    // ============================================================

    public async Task<IReadOnlyList<WorkflowTaskActionDto>> GetActionsAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await db.WorkflowTasks.AsNoTracking()
            .Include(x => x.WorkflowNode)
            .Include(x => x.WorkflowInstance)
                .ThenInclude(x => x.WorkflowDefinition)
                    .ThenInclude(x => x.Nodes)
            .Include(x => x.WorkflowInstance)
                .ThenInclude(x => x.WorkflowDefinition)
                    .ThenInclude(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.Status != WorkflowTaskStatus.Open)
            return Array.Empty<WorkflowTaskActionDto>();

        var canAct = task.AssignedToUserId == currentUser.UserId;

        if (!canAct && task.AssignedToUserId is null && !string.IsNullOrWhiteSpace(task.AssignedRoleCode))
        {
            canAct = await db.DepartmentMemberships.AsNoTracking().AnyAsync(x =>
                x.UserId == currentUser.UserId &&
                x.DepartmentId == task.DepartmentId &&
                x.RoleCode == task.AssignedRoleCode, cancellationToken);
        }

        if (!canAct)
            throw new UnauthorizedAccessException("This task is not assigned to you or your role queue.");

        var definition = task.WorkflowInstance.WorkflowDefinition;
        var outgoing = definition.Transitions
            .Where(x => x.SourceNodeId == task.WorkflowNodeId)
            .ToList();

        var actions = new List<WorkflowTaskActionDto>();

        foreach (var transition in outgoing)
        {
            var target = definition.Nodes.Single(x => x.Id == transition.TargetNodeId);

            actions.Add(new WorkflowTaskActionDto(
                transition.OutcomeKey,
                string.IsNullOrWhiteSpace(transition.Label)
                    ? BuildDefaultActionLabel(transition.OutcomeKey, target.Name)
                    : transition.Label!,
                target.Id,
                target.Name,
                target.Type,
                target.RoleCode,
                RequiresSpecificAssignee(target)));
        }

        return actions;
    }

    // ============================================================
    // FORM  (current fields + previous steps with attachments)
    // ============================================================

    public async Task<WorkflowTaskFormDto> GetFormAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await db.WorkflowTasks
            .AsNoTracking()
            .Include(x => x.WorkflowNode)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(f => f.AttachmentConfig)
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.Status != WorkflowTaskStatus.Open)
            throw new InvalidOperationException("Task is no longer open.");

        var canAct = task.AssignedToUserId == currentUser.UserId;

        if (!canAct && task.AssignedToUserId is null &&
            !string.IsNullOrWhiteSpace(task.AssignedRoleCode))
        {
            canAct = await db.DepartmentMemberships
                .AsNoTracking()
                .AnyAsync(
                    x => x.UserId == currentUser.UserId &&
                         x.DepartmentId == task.DepartmentId &&
                         x.RoleCode == task.AssignedRoleCode,
                    cancellationToken);
        }

        if (!canAct)
            throw new UnauthorizedAccessException(
                "This task is not assigned to you or your role queue.");

        // ---- CURRENT FIELDS (with attachment config) ----
        var currentFields = task.WorkflowNode.Fields
            .OrderBy(x => x.DisplayOrder)
            .Select(field => new WorkflowTaskFieldDto(
                field.Id,
                field.FieldKey,
                field.Label,
                field.FieldType,
                field.Placeholder,
                field.IsRequired,
                field.DisplayOrder,
                ParseOptions(field.OptionsJson),
                field.AttachmentConfig is null
                    ? null
                    : ParseAllowedFileTypes(field.AttachmentConfig.AllowedFileTypesJson),
                field.AttachmentConfig?.MaxFileSizeMb,
                field.AttachmentConfig?.AllowMultiple))
            .ToList();

        var completedTasks = await db.WorkflowTasks
            .AsNoTracking()
            .Include(x => x.WorkflowNode)
            .Where(x => x.ComplaintId == task.ComplaintId &&
                        x.Status == WorkflowTaskStatus.Completed)
            .OrderBy(x => x.CompletedAtUtc)
            .ToListAsync(cancellationToken);

        var completedByUserIds = completedTasks
            .Where(x => x.CompletedByUserId.HasValue)
            .Select(x => x.CompletedByUserId!.Value)
            .Distinct()
            .ToList();

        var completedByUsers = await db.Users
            .AsNoTracking()
            .Where(x => completedByUserIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

        var completedTaskIds = completedTasks.Select(x => x.Id).ToList();

        var responses = await db.WorkflowFieldResponses
            .AsNoTracking()
            .Include(x => x.WorkflowNodeField)
            .Where(x => completedTaskIds.Contains(x.WorkflowTaskId))
            .ToListAsync(cancellationToken);

        // --- Load attachments for these responses in one query ---
        var responseIds = responses.Select(r => r.Id).ToList();

        var attachments = await db.WorkflowFieldAttachments
            .AsNoTracking()
            .Where(a => responseIds.Contains(a.WorkflowFieldResponseId))
           .Select(a => new
{
    a.Id,
    a.WorkflowFieldResponseId,
    a.FileName,
    a.ContentType,
    a.FileSize,          
})
            .ToListAsync(cancellationToken);

        var attachmentsByResponseId = attachments
            .GroupBy(a => a.WorkflowFieldResponseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var previousSteps = completedTasks
            .Select(previousTask =>
            {
                var taskResponses = responses
                    .Where(x => x.WorkflowTaskId == previousTask.Id)
                    .OrderBy(x => x.WorkflowNodeField.DisplayOrder)
                    .Select(response =>
                    {
                        var fieldType = response.WorkflowNodeField.FieldType;

                        object? value;

                        if (string.Equals(fieldType, "attachment", StringComparison.OrdinalIgnoreCase))
                        {
                            if (attachmentsByResponseId.TryGetValue(response.Id, out var list) && list.Count > 0)
                            {
                                var shaped = list.Select(a => new
                                {
                                    attachmentId = a.Id,
                                    fileName = a.FileName,
                                    contentType = a.ContentType,
                                }).ToList();

                                value = shaped.Count == 1 ? shaped[0] : shaped;
                            }
                            else
                            {
                                value = null;
                            }
                        }
                        else
                        {
                            value = ParseValue(response.ValueJson);
                        }

                        return new PreviousWorkflowFieldDto(
                            response.WorkflowNodeField.FieldKey,
                            response.WorkflowNodeField.Label,
                            fieldType,
                            value);
                    })
                    .ToList();

                var submittedBy =
                    previousTask.CompletedByUserId.HasValue &&
                    completedByUsers.TryGetValue(previousTask.CompletedByUserId.Value, out var userName)
                        ? userName
                        : null;

                return new PreviousWorkflowStepDto(
                    previousTask.Id,
                    previousTask.WorkflowNode.Name,
                    previousTask.AssignedRoleCode,
                    submittedBy,
                    previousTask.CompletedAtUtc,
                    taskResponses);
            })
            .Where(x => x.Fields.Count > 0)
            .ToList();

        return new WorkflowTaskFormDto(
            task.Id,
            task.WorkflowNode.Name,
            currentFields,
            previousSteps);
    }

    // ============================================================
    // COMPLETE (delegates to runtime)
    // ============================================================

    public Task CompleteAsync(
        Guid taskId,
        CompleteWorkflowTaskRequest request,
        IReadOnlyList<IFormFile>? files,
        CancellationToken cancellationToken) =>
        runtime.CompleteTaskAsync(
            taskId,
            currentUser.UserId,
            request,
            files,
            cancellationToken);

    // ============================================================
    // REASSIGN
    // ============================================================

    public async Task ReassignAsync(Guid taskId, ReassignWorkflowTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await db.WorkflowTasks
            .Include(x => x.WorkflowNode)
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.Status != WorkflowTaskStatus.Open)
            throw new InvalidOperationException("Only open tasks can be reassigned.");

        var actorCanManage = await db.DepartmentMemberships.AnyAsync(x =>
            x.UserId == currentUser.UserId &&
            x.DepartmentId == task.DepartmentId &&
            (x.RoleCode == "TeamLead" || x.RoleCode == "UnitHead"),
            cancellationToken);

        if (!actorCanManage)
            throw new UnauthorizedAccessException("Only a TeamLead or UnitHead can reassign this task.");

        if (!string.IsNullOrWhiteSpace(task.AssignedRoleCode))
        {
            var targetHasRole = await db.DepartmentMemberships.AnyAsync(x =>
                x.UserId == request.UserId &&
                x.DepartmentId == task.DepartmentId &&
                x.RoleCode == task.AssignedRoleCode, cancellationToken);

            if (!targetHasRole)
                throw new InvalidOperationException($"Target user does not have role '{task.AssignedRoleCode}'.");
        }

        task.AssignedToUserId = request.UserId;
        task.UpdatedAtUtc = DateTime.UtcNow;

        db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = task.ComplaintId,
            ActorUserId = currentUser.UserId,
            EventType = "WorkflowTaskReassigned",
            Message = string.IsNullOrWhiteSpace(request.Comment)
                ? $"Step '{task.WorkflowNode.Name}' was reassigned."
                : $"Step '{task.WorkflowNode.Name}' was reassigned. Comment: {request.Comment}"
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static string BuildDefaultActionLabel(string? outcomeKey, string targetName)
    {
        if (!string.IsNullOrWhiteSpace(outcomeKey))
        {
            var readable = string.Concat(outcomeKey.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
            return char.ToUpperInvariant(readable[0]) + readable[1..];
        }
        return $"Continue to {targetName}";
    }

    private static bool RequiresSpecificAssignee(WorkflowNode node)
    {
        if (string.IsNullOrWhiteSpace(node.ConfigJson)) return false;

        try
        {
            using var document = JsonDocument.Parse(node.ConfigJson);
            if (!document.RootElement.TryGetProperty("assignmentMode", out var value)) return false;
            return string.Equals(value.GetString(), "specific", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyList<string> ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static object? ParseValue(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<object>(json);
        }
        catch
        {
            return json;
        }
    }

    // ============================================================
    // Attachment config JSON parser
    // ============================================================

    private static IReadOnlyList<string>? ParseAllowedFileTypes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return null;
        }
    }

    internal static Guid? ExtractFieldIdFromPartName(string partName)
    {
        const string bracket = "files[";
        if (partName.StartsWith(bracket, StringComparison.Ordinal) &&
            partName.EndsWith("]", StringComparison.Ordinal))
        {
            var inner = partName[bracket.Length..^1];
            return Guid.TryParse(inner, out var g1) ? g1 : null;
        }

        const string underscore = "files_";
        if (partName.StartsWith(underscore, StringComparison.Ordinal))
        {
            var inner = partName[underscore.Length..];
            return Guid.TryParse(inner, out var g2) ? g2 : null;
        }

        return null;
    }
}