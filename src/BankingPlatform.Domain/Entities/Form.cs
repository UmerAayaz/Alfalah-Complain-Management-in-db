using BankingPlatform.Domain.Common;

namespace BankingPlatform.Domain.Entities;

public sealed class Form : EntityBase
{
    public string FormKey { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid CreatedByUserId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? CategoryId { get; set; }

    public ICollection<FormField> Fields { get; set; } = new List<FormField>();
}

public sealed class FormField : EntityBase
{
    public Guid FormId { get; set; }
    public Form Form { get; set; } = null!;

    public Guid LibraryFieldId { get; set; }
    public LibraryField LibraryField { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public string? LabelOverride { get; set; }

    public bool? IsRequiredOverride { get; set; }
}