using BankingPlatform.Domain.Common;

namespace BankingPlatform.Domain.Entities;

public sealed class WorkflowNodeField : EntityBase
{
    public Guid WorkflowNodeId { get; set; }
    public WorkflowNode WorkflowNode { get; set; } = null!;

    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text";
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string? OptionsJson { get; set; }

    /// <summary>
    /// Attachment configuration. Only present when FieldType == "attachment".
    /// </summary>
    // public WorkflowNodeFieldAttachmentConfig? AttachmentConfig { get; set; }
}

// public sealed class WorkflowNodeFieldAttachmentConfig : EntityBase
// {
//     /// <summary>
//     /// FK to the parent WorkflowNodeField. One-to-one.
//     /// Only attachment-type fields have a row here.
//     /// </summary>
//     public Guid WorkflowNodeFieldId { get; set; }

//     public WorkflowNodeField WorkflowNodeField { get; set; } = null!;

//     /// <summary>
//     /// JSON array of allowed file extensions, e.g. ["pdf","jpg","png"].
//     /// Null or empty means "any file type is allowed".
//     /// </summary>
//     public string? AllowedFileTypesJson { get; set; }

//     /// <summary>
//     /// Maximum allowed file size in megabytes.
//     /// Null means "use the system default" (currently 5 MB).
//     /// </summary>
//     public int? MaxFileSizeMb { get; set; }

//     /// <summary>
//     /// Whether multiple files can be uploaded for this field.
//     /// Null or false means single-file only.
//     /// </summary>
//     public bool? AllowMultiple { get; set; }
// }
public sealed class WorkflowFieldResponse : EntityBase
{
    public Guid ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public Guid WorkflowTaskId { get; set; }
    public WorkflowTask WorkflowTask { get; set; } = null!;
    public Guid WorkflowNodeId { get; set; }
    public WorkflowNode WorkflowNode { get; set; } = null!;

    // Inline field (nullable — form-attached tasks use LibraryFieldId instead)
    // // public Guid? WorkflowNodeFieldId { get; set; }
    // public WorkflowNodeField? WorkflowNodeField { get; set; }

    // Library field (nullable — only set when form-attached task)
    public Guid? LibraryFieldId { get; set; }
    public LibraryField? LibraryField { get; set; }

    public Guid SubmittedByUserId { get; set; }
    public AppUser SubmittedByUser { get; set; } = null!;
    public string? ValueJson { get; set; }
    public string? Label { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}