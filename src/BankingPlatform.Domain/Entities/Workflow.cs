using BankingPlatform.Domain.Common;
using BankingPlatform.Domain.Enums;

namespace BankingPlatform.Domain.Entities;

public sealed class WorkflowDefinition : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public ComplaintCategory Category { get; set; } = null!;
    public int Version { get; set; } = 1;
    public WorkflowDefinitionStatus Status { get; set; } = WorkflowDefinitionStatus.Draft;
    public string DesignerJson { get; set; } = "{}";
    public Guid CreatedByUserId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public ICollection<WorkflowNode> Nodes { get; set; } = new List<WorkflowNode>();
    public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
    // NEW: default priority for workflow
public int DefaultPriority { get; set; } = 1;   // 0=Low, 1=Normal, 2=High, 3=Critical
   public Guid? ProductId { get; set; }
public Product? Product { get; set; }
}

public sealed class WorkflowNode : EntityBase
{
    public Guid WorkflowDefinitionId { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public WorkflowNodeType Type { get; set; }
    public string? RoleCode { get; set; }
    public int? SlaHours { get; set; }
    public string? EscalationRoleCode { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public string? ConfigJson { get; set; }
    public Guid? FormId { get; set; }        // ← ye add karo
    public ICollection<WorkflowNodeField> Fields { get; set; } = new List<WorkflowNodeField>();
}

public sealed class WorkflowTransition : EntityBase
{
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    public Guid SourceNodeId { get; set; }
    public WorkflowNode SourceNode { get; set; } = null!;
    public Guid TargetNodeId { get; set; }
    public WorkflowNode TargetNode { get; set; } = null!;
    public string? OutcomeKey { get; set; }
    public string? Label { get; set; }
}

public sealed class WorkflowInstance : EntityBase
{
    public Guid ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    public Guid? CurrentNodeId { get; set; }
    public WorkflowNode? CurrentNode { get; set; }
    public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Running;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class WorkflowTask : EntityBase
{
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    public Guid WorkflowNodeId { get; set; }
    public WorkflowNode WorkflowNode { get; set; } = null!;
    public Guid ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public string? AssignedRoleCode { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }
    public WorkflowTaskStatus Status { get; set; } = WorkflowTaskStatus.Open;
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DueAtUtc { get; set; }
    public DateTime? EscalatedAtUtc { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletionComment { get; set; }
}
