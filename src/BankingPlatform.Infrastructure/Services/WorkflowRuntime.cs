using System.Text.Json;
using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace BankingPlatform.Infrastructure.Services;

public sealed class SqlWorkflowRuntime(
    AppDbContext db) : IWorkflowRuntime
{
    private readonly AppDbContext _db = db;

    // ============================================================
    // START
    // ============================================================

    public async Task StartAsync(Complaint complaint, CancellationToken cancellationToken)
    {
        var definition = await db.WorkflowDefinitions
            .Include(x => x.Nodes)
            .Include(x => x.Transitions)
            .Where(x => x.CategoryId == complaint.CategoryId && x.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No published workflow exists for this complaint category.");

        var instance = new WorkflowInstance
        {
            ComplaintId = complaint.Id,
            Complaint = complaint,
            WorkflowDefinitionId = definition.Id,
            Status = WorkflowInstanceStatus.Running
        };
        db.WorkflowInstances.Add(instance);
        complaint.CurrentWorkflowInstanceId = instance.Id;
        complaint.Status = ComplaintStatus.InProgress;

        var start = definition.Nodes.FirstOrDefault(x => x.Type == WorkflowNodeType.Start)
            ?? throw new InvalidOperationException(
                $"Workflow '{definition.Name}' (v{definition.Version}) is missing its Start node. " +
                "Please open the workflow designer, add a Start node, and re-publish.");

        await MoveFromNodeAsync(
            instance, definition, start,
            outcomeKey: null, nextAssigneeUserId: null,
            actorUserId: complaint.CreatedByUserId,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    // ============================================================
    // COMPLETE TASK — supports both form-based and inline fields
    // ============================================================

    public async Task CompleteTaskAsync(
        Guid taskId,
        Guid actorUserId,
        CompleteWorkflowTaskRequest request,
        IReadOnlyList<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        var task = await db.WorkflowTasks
            .Include(x => x.WorkflowNode)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(f => f.AttachmentConfig)
            .Include(x => x.WorkflowInstance)
                .ThenInclude(x => x.WorkflowDefinition)
                    .ThenInclude(x => x.Nodes)
            .Include(x => x.WorkflowInstance)
                .ThenInclude(x => x.WorkflowDefinition)
                    .ThenInclude(x => x.Transitions)
            .Include(x => x.Complaint)
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.Status != WorkflowTaskStatus.Open)
            throw new InvalidOperationException("Task is no longer open.");

        var canAct = task.AssignedToUserId == actorUserId;
        if (!canAct && task.AssignedToUserId is null && !string.IsNullOrWhiteSpace(task.AssignedRoleCode))
        {
            canAct = await db.DepartmentMemberships.AnyAsync(x =>
                x.UserId == actorUserId &&
                x.DepartmentId == task.DepartmentId &&
                x.RoleCode == task.AssignedRoleCode, cancellationToken);
        }

        if (!canAct)
            throw new UnauthorizedAccessException("This task is not assigned to you or your role queue.");

        if (request.NextAssigneeUserId.HasValue)
        {
            var userExists = await db.Users.AnyAsync(x =>
                x.Id == request.NextAssigneeUserId.Value && x.IsActive, cancellationToken);

            if (!userExists)
                throw new ArgumentException("Selected assignee does not exist or is inactive.");
        }

        // ============================================================
        // LOAD ATTACHED FORM (if any)
        // ============================================================
        Form? attachedForm = null;

        if (task.WorkflowNode.FormId.HasValue)
        {
            attachedForm = await db.Forms
                .Include(x => x.Fields)
                    .ThenInclude(x => x.LibraryField)
                        .ThenInclude(lf => lf.AttachmentConfig)
                .FirstOrDefaultAsync(
                    x => x.Id == task.WorkflowNode.FormId.Value,
                    cancellationToken);
        }

        var isFormBased = attachedForm is not null && attachedForm.Fields.Count > 0;

        // ============================================================
        // UNIFIED FIELD LIST — either from form OR from node
        // ============================================================
        // Shape: Id, Label, FieldType, IsRequired
        var unifiedFields = isFormBased
            ? attachedForm!.Fields
                .OrderBy(x => x.DisplayOrder)
                .Select(ff => new UnifiedField(
                    ff.LibraryFieldId,
                    ff.LabelOverride ?? ff.LibraryField.Label,
                    ff.LibraryField.FieldType,
                    ff.IsRequiredOverride ?? ff.LibraryField.IsRequired))
                .ToList()
            : task.WorkflowNode.Fields
                .OrderBy(x => x.DisplayOrder)
                .Select(f => new UnifiedField(
                    f.Id,
                    f.Label,
                    f.FieldType,
                    f.IsRequired))
                .ToList();

        var validFieldIds = unifiedFields.Select(x => x.Id).ToHashSet();

        var suppliedAnswers = request.FieldAnswers ?? Array.Empty<WorkflowFieldAnswerRequest>();

        var answerLookup = suppliedAnswers
            .GroupBy(x => x.FieldId)
            .ToDictionary(x => x.Key, x => x.Last());

        // ---- Validate submitted field IDs ----
        foreach (var answer in suppliedAnswers)
        {
            if (!validFieldIds.Contains(answer.FieldId))
            {
                throw new ArgumentException(
                    "One of the submitted fields does not belong to this workflow step.");
            }
        }

        // ---- Required-field validation ----
        foreach (var requiredField in unifiedFields.Where(x => x.IsRequired))
        {
            if (string.Equals(requiredField.FieldType, "attachment", StringComparison.OrdinalIgnoreCase))
            {
                var hasFile = files?.Any(x =>
                    x.Length > 0 &&
                    (string.Equals(x.Name, $"files_{requiredField.Id}", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.Name, $"files[{requiredField.Id}]", StringComparison.OrdinalIgnoreCase)))
                    ?? false;

                if (!hasFile)
                    throw new ArgumentException($"'{requiredField.Label}' is required.");

                continue;
            }

            if (!answerLookup.TryGetValue(requiredField.Id, out var answer) ||
                IsEmpty(answer.Value))
            {
                throw new ArgumentException($"'{requiredField.Label}' is required.");
            }
        }

        // ============================================================
        // PERSIST RESPONSES + ATTACHMENTS
        // ============================================================
        foreach (var field in unifiedFields)
        {
            answerLookup.TryGetValue(field.Id, out var answer);

            var valueJson = answer?.Value is null
                ? null
                : answer.Value.Value.GetRawText();

            var response = new WorkflowFieldResponse
            {
                ComplaintId = task.ComplaintId,
                WorkflowTaskId = task.Id,
                WorkflowNodeId = task.WorkflowNodeId,
                SubmittedByUserId = actorUserId,
                Label = field.Label,
                ValueJson = valueJson,
                SubmittedAtUtc = DateTime.UtcNow,

                // Set the right FK depending on the source
                WorkflowNodeFieldId = isFormBased ? null : field.Id,
                LibraryFieldId = isFormBased ? field.Id : null,
            };

            db.WorkflowFieldResponses.Add(response);

            if (string.Equals(field.FieldType, "attachment", StringComparison.OrdinalIgnoreCase))
            {
                var fieldFiles = files?
                    .Where(x =>
                        string.Equals(x.Name, $"files_{field.Id}", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Name, $"files[{field.Id}]", StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    ?? new List<IFormFile>();

                foreach (var file in fieldFiles)
                {
                    if (file.Length <= 0) continue;

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms, cancellationToken);
                    var content = ms.ToArray();

                    string? hash;
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                    {
                        hash = Convert.ToHexString(sha.ComputeHash(content)).ToLowerInvariant();
                    }

                    db.WorkflowFieldAttachments.Add(new WorkflowFieldAttachment
                    {
                        Id = Guid.NewGuid(),
                        WorkflowFieldResponseId = response.Id,
                        FileName = file.FileName,
                        ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                            ? "application/octet-stream"
                            : file.ContentType,
                        FileSize = file.Length,
                        Content = content,
                        Sha256Hash = hash,
                        UploadedByUserId = actorUserId,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        // ============================================================
        // COMPLETE TASK
        // ============================================================
        task.Status = WorkflowTaskStatus.Completed;
        task.CompletedAtUtc = DateTime.UtcNow;
        task.CompletedByUserId = actorUserId;
        task.CompletionComment = request.Comment;
        task.UpdatedAtUtc = DateTime.UtcNow;

        db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = task.ComplaintId,
            ActorUserId = actorUserId,
            EventType = "WorkflowTaskCompleted",
            Message = $"Completed workflow step '{task.WorkflowNode.Name}'."
        });

        task.WorkflowInstance.Complaint = task.Complaint;
        var definition = task.WorkflowInstance.WorkflowDefinition;

        await MoveFromNodeAsync(
            task.WorkflowInstance, definition, task.WorkflowNode,
            request.OutcomeKey, request.NextAssigneeUserId,
            actorUserId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    // ============================================================
    // TRANSITION / NODE ENTRY
    // ============================================================

    private async Task MoveFromNodeAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        WorkflowNode sourceNode,
        string? outcomeKey,
        Guid? nextAssigneeUserId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var outgoing = definition.Transitions
            .Where(x => x.SourceNodeId == sourceNode.Id)
            .ToList();

        WorkflowTransition? transition;

        if (!string.IsNullOrWhiteSpace(outcomeKey))
        {
            transition = outgoing.FirstOrDefault(x =>
                string.Equals(x.OutcomeKey, outcomeKey, StringComparison.OrdinalIgnoreCase))
                ?? outgoing.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.OutcomeKey));
        }
        else
        {
            transition = outgoing.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.OutcomeKey));
            if (transition is null && outgoing.Count == 1) transition = outgoing[0];
        }

        if (transition is null)
            throw new InvalidOperationException(
                $"No transition matched outcome '{outcomeKey ?? "<default>"}' from node '{sourceNode.Name}'.");

        var target = definition.Nodes.FirstOrDefault(x => x.Id == transition.TargetNodeId)
            ?? throw new InvalidOperationException(
                $"Workflow '{definition.Name}' has a transition from '{sourceNode.Name}' " +
                "pointing to a node that no longer exists. Please re-publish the workflow.");

        await EnterNodeAsync(instance, definition, target, nextAssigneeUserId, actorUserId, cancellationToken);
    }

    private async Task EnterNodeAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        WorkflowNode node,
        Guid? explicitAssigneeUserId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        instance.CurrentNodeId = node.Id;
        instance.UpdatedAtUtc = DateTime.UtcNow;

        if (node.Type == WorkflowNodeType.End)
        {
            instance.Status = WorkflowInstanceStatus.Completed;
            instance.CompletedAtUtc = DateTime.UtcNow;
            instance.Complaint.Status = ComplaintStatus.Resolved;
            instance.Complaint.ResolvedAtUtc = DateTime.UtcNow;
            instance.Complaint.UpdatedAtUtc = DateTime.UtcNow;

            db.ComplaintEvents.Add(new ComplaintEvent
            {
                ComplaintId = instance.ComplaintId,
                ActorUserId = actorUserId,
                EventType = "ComplaintResolved",
                Message = $"Complaint reached end step '{node.Name}'."
            });
            return;
        }

        if (node.Type == WorkflowNodeType.Start)
        {
            await MoveFromNodeAsync(instance, definition, node, null, explicitAssigneeUserId, actorUserId, cancellationToken);
            return;
        }

        if (node.Type != WorkflowNodeType.HumanTask)
            throw new NotSupportedException($"Node type {node.Type} is not implemented yet.");

        if (RequiresSpecificAssignee(node) && !explicitAssigneeUserId.HasValue)
            throw new InvalidOperationException($"Step '{node.Name}' requires a specific user assignment.");

        if (explicitAssigneeUserId.HasValue && !string.IsNullOrWhiteSpace(node.RoleCode))
        {
            var hasRole = await db.DepartmentMemberships.AnyAsync(x =>
                x.UserId == explicitAssigneeUserId.Value &&
                x.DepartmentId == definition.DepartmentId &&
                x.RoleCode == node.RoleCode, cancellationToken);

            if (!hasRole)
                throw new InvalidOperationException(
                    $"Selected user does not have role '{node.RoleCode}' in this department.");
        }

        var now = DateTime.UtcNow;
        var task = new WorkflowTask
        {
            WorkflowInstanceId = instance.Id,
            WorkflowNodeId = node.Id,
            ComplaintId = instance.ComplaintId,
            DepartmentId = definition.DepartmentId,
            AssignedRoleCode = node.RoleCode,
            AssignedToUserId = explicitAssigneeUserId,
            Status = WorkflowTaskStatus.Open,
            OpenedAtUtc = now,
            DueAtUtc = node.SlaHours.HasValue ? now.AddHours(node.SlaHours.Value) : null
        };
        db.WorkflowTasks.Add(task);

        db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = instance.ComplaintId,
            ActorUserId = actorUserId,
            EventType = "WorkflowTaskOpened",
            Message = explicitAssigneeUserId.HasValue
                ? $"Step '{node.Name}' assigned to a specific user."
                : $"Step '{node.Name}' entered the '{node.RoleCode}' queue."
        });
    }

    // ============================================================
    // HELPERS
    // ============================================================

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

    private static bool IsEmpty(JsonElement? value)
    {
        if (!value.HasValue) return true;
        var element = value.Value;

        if (element.ValueKind == JsonValueKind.Null ||
            element.ValueKind == JsonValueKind.Undefined)
            return true;

        if (element.ValueKind == JsonValueKind.String)
            return string.IsNullOrWhiteSpace(element.GetString());

        if (element.ValueKind == JsonValueKind.Array)
            return element.GetArrayLength() == 0;

        return false;
    }

    // ============================================================
    // INTERNAL HELPER TYPE
    // ============================================================
    private sealed record UnifiedField(
        Guid Id,
        string Label,
        string FieldType,
        bool IsRequired);
}