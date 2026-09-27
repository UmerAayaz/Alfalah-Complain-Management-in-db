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

        var start = definition.Nodes.Single(x => x.Type == WorkflowNodeType.Start);
        await MoveFromNodeAsync(instance, definition, start, outcomeKey: null, nextAssigneeUserId: null, actorUserId: complaint.CreatedByUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    // ============================================================
    // COMPLETE TASK
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

        var fields = task.WorkflowNode.Fields
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        var suppliedAnswers = request.FieldAnswers ?? Array.Empty<WorkflowFieldAnswerRequest>();

        var answerLookup = suppliedAnswers
            .GroupBy(x => x.FieldId)
            .ToDictionary(x => x.Key, x => x.Last());

        Console.WriteLine("=== FIELD ID DEBUG BACKEND ===");
        foreach (var field in fields)
        {
            Console.WriteLine($"Backend Field: Id={field.Id}, Label={field.Label}, Type={field.FieldType}");
        }
        foreach (var answer in suppliedAnswers)
        {
            Console.WriteLine($"Submitted Field: Id={answer.FieldId}");
        }

        foreach (var answer in suppliedAnswers)
        {
            if (fields.All(x => x.Id != answer.FieldId))
            {
                throw new ArgumentException(
                    "One of the submitted fields does not belong to this workflow step.");
            }
        }

        Console.WriteLine("=== ATTACHMENT DEBUG ===");
        Console.WriteLine($"Files count: {files?.Count ?? 0}");
        if (files != null)
        {
            foreach (var file in files)
            {
                Console.WriteLine($"File: Name={file.Name}, FileName={file.FileName}, Size={file.Length}");
            }
        }

        // --- Required-field validation (both attachment and normal) ---
        foreach (var requiredField in fields.Where(x => x.IsRequired))
        {
            if (string.Equals(requiredField.FieldType, "attachment", StringComparison.OrdinalIgnoreCase))
            {
                // Accept "files_<fieldId>" OR "files[<fieldId>]"
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

        // --- Persist field responses + attachments ---
        foreach (var field in fields)
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
                WorkflowNodeFieldId = field.Id,
                SubmittedByUserId = actorUserId,
                Label = field.Label,
                ValueJson = valueJson,
                SubmittedAtUtc = DateTime.UtcNow
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
                    if (file.Length <= 0)
                        continue;

                    // Read file bytes into memory
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms, cancellationToken);
                    var content = ms.ToArray();

                    // SHA-256 for integrity
                    string? hash = null;
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
            task.WorkflowInstance,
            definition,
            task.WorkflowNode,
            request.OutcomeKey,
            request.NextAssigneeUserId,
            actorUserId,
            cancellationToken);

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

        var target = definition.Nodes.Single(x => x.Id == transition.TargetNodeId);
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
}