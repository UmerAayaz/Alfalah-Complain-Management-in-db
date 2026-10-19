using BankingPlatform.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BankingPlatform.Application.DTOs;

// ============================================================
// COMPLAINT (input)
// ============================================================
// CustomerReference ab auto-generate hota hai backend se
// (format: REF-YYYYMMDD-XXXX) — user se nahi liya jata
// AutoReference deleted — ComplaintNumber hi unique ID hai
// ============================================================

public sealed record CreateComplaintRequest(
    Guid CategoryId,
    string Subject,
    string Description,
    string? CustomerReference = null,
    ComplaintPriority Priority = ComplaintPriority.Normal,
    Guid? ProductId = null);

    
public sealed record ComplaintListItemDto(
    Guid Id,
    string ComplaintNumber,
    string? CustomerReference,
    string Category,
    string Subject,
    ComplaintStatus Status,
    ComplaintPriority Priority,
    DateTime CreatedAtUtc);

public sealed record ComplaintEventDto(
    DateTime CreatedAtUtc,
    string EventType,
    string Message,
    string? ActorName);

public sealed record ComplaintDetailsDto(
    Guid Id,
    string ComplaintNumber,
    string Category,
    string Subject,
    string Description,
    string? CustomerReference,
    ComplaintStatus Status,
    ComplaintPriority Priority,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    IReadOnlyList<ComplaintEventDto> Timeline,
    string? ProductName = null);

// ============================================================
// WORKFLOW DEFINITION (designer input)
// ============================================================

public sealed record WorkflowNodeRequest(
    string Key,
    string Name,
    WorkflowNodeType Type,
    string? RoleCode,
    int? SlaHours,
    string? EscalationRoleCode,
    decimal X,
    decimal Y,
    string? ConfigJson = null,
    IReadOnlyList<WorkflowNodeFieldRequest>? Fields = null,
    Guid? FormId = null);

public sealed record WorkflowEdgeRequest(
    string SourceKey,
    string TargetKey,
    string? OutcomeKey = null,
    string? Label = null);

public sealed record CreateWorkflowDefinitionRequest(
    string Name,
    Guid DepartmentId,
    Guid CategoryId,
    IReadOnlyList<WorkflowNodeRequest> Nodes,
    IReadOnlyList<WorkflowEdgeRequest> Edges,
    int DefaultPriority = 1,
    Guid? ProductId = null);

public sealed record WorkflowDefinitionDto(
    Guid Id,
    string Name,
    Guid DepartmentId,
    Guid CategoryId,
    int Version,
    WorkflowDefinitionStatus Status,
    string DesignerJson,
    DateTime? PublishedAtUtc,
    int DefaultPriority = 1,
    Guid? ProductId = null,
    string? ProductName = null);

// ============================================================
// WORKFLOW TASKS
// ============================================================

public sealed record WorkflowTaskDto(
    Guid Id,
    Guid ComplaintId,
    Guid DepartmentId,
    string ComplaintNumber,
    string ComplaintSubject,
    string NodeName,
    string? AssignedRoleCode,
    Guid? AssignedToUserId,
    string? AssignedToUserName,
    DateTime OpenedAtUtc,
    DateTime? DueAtUtc,
    DateTime? EscalatedAtUtc);

public sealed record WorkflowTaskActionDto(
    string? OutcomeKey,
    string Label,
    Guid TargetNodeId,
    string TargetNodeName,
    WorkflowNodeType TargetNodeType,
    string? TargetRoleCode,
    bool RequiresAssignee);

public sealed record CompleteWorkflowTaskRequest(
    string? OutcomeKey,
    Guid? NextAssigneeUserId,
    string? Comment,
    IReadOnlyList<WorkflowFieldAnswerRequest>? FieldAnswers = null);

// ============================================================
// WORKFLOW NODE FIELD (designer → entity)
// ============================================================

public sealed record WorkflowNodeFieldRequest(
    string FieldKey,
    string Label,
    string FieldType,
    string? Placeholder,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyList<string>? Options,

    // Attachment configuration (only used when FieldType == "attachment")
    IReadOnlyList<string>? AllowedFileTypes = null,
    int? MaxFileSizeMb = null,
    bool? AllowMultiple = null);

// ============================================================
// REFERENCE / USER DTOs
// ============================================================

public sealed record ReassignWorkflowTaskRequest(Guid UserId, string? Comment);

public sealed record ReferenceItemDto(Guid Id, string? Code, string Name);

public sealed record UserReferenceDto(
    Guid Id,
    string EmployeeCode,
    string DisplayName,
    string Email,
    string RoleCode);

public sealed record CurrentUserMembershipDto(
    Guid DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    string RoleCode);

public sealed record CurrentUserDto(
    Guid Id,
    string EmployeeCode,
    string DisplayName,
    string Email,
    IReadOnlyList<CurrentUserMembershipDto> Memberships);

// ============================================================
// COMPLAINT ATTACHMENTS
// ============================================================

public sealed record ComplaintAttachmentDto(
    Guid Id,
    Guid ComplaintId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? UploadedByUserId,
    DateTime UploadedAtUtc);

public sealed record UploadAttachmentInput(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);

// ============================================================
// WORKFLOW TASK FORM (entity → frontend)
// ============================================================

public sealed record WorkflowTaskFieldDto(
    Guid Id,
    string FieldKey,
    string Label,
    string FieldType,
    string? Placeholder,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyList<string> Options,
    IReadOnlyList<string>? AllowedFileTypes = null,
    int? MaxFileSizeMb = null,
    bool? AllowMultiple = null,
    string? RegexPattern = null,
    string? RegexMessage = null,
    int? MinLength = null,
    int? MaxLength = null,
    decimal? MinValue = null,
    decimal? MaxValue = null);

public sealed record PreviousWorkflowFieldDto(
    string FieldKey,
    string Label,
    string FieldType,
    object? Value);

public sealed record PreviousWorkflowStepDto(
    Guid WorkflowTaskId,
    string NodeName,
    string? RoleCode,
    string? SubmittedBy,
    DateTime? SubmittedAtUtc,
    IReadOnlyList<PreviousWorkflowFieldDto> Fields);

public sealed record WorkflowTaskFormDto(
    Guid WorkflowTaskId,
    string NodeName,
    Guid? FormId,
    string? FormName,
    IReadOnlyList<WorkflowTaskFieldDto> CurrentFields,
    IReadOnlyList<PreviousWorkflowStepDto> PreviousSteps);

// ============================================================
// FIELD ANSWER (frontend → backend)
// ============================================================

public sealed record WorkflowFieldAnswerRequest(
    [property: JsonPropertyName("fieldId")] Guid FieldId,
    [property: JsonPropertyName("value")] JsonElement? Value)
{
    /// <summary>Convenience: get the value as a string when the JSON is a string.</summary>
    public string? AsString() => Value switch
    {
        null => null,
        { ValueKind: JsonValueKind.Null } => null,
        { ValueKind: JsonValueKind.String } v => v.GetString(),
        _ => Value.Value.ToString()
    };
}