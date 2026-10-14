using BankingPlatform.Domain.Common;
using BankingPlatform.Domain.Enums;

namespace BankingPlatform.Domain.Entities;

public sealed class Complaint : EntityBase
{
    public string ComplaintNumber { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public ComplaintCategory Category { get; set; } = null!;
    public string? CustomerReference { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintPriority Priority { get; set; } = ComplaintPriority.Normal;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public Guid CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;
    public Guid? CurrentWorkflowInstanceId { get; set; }
    public WorkflowInstance? CurrentWorkflowInstance { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    // Selected product
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    // Auto-generated reference
    public string? AutoReference { get; set; }
}

public sealed class ComplaintEvent : EntityBase
{
    public Guid ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public Guid? ActorUserId { get; set; }
    public AppUser? ActorUser { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DataJson { get; set; }
}