using BankingPlatform.Domain.Common;

namespace BankingPlatform.Domain.Entities;

public sealed class LibraryField : EntityBase
{
    public string FieldKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string FieldType { get; set; } = "text";

    public string? Placeholder { get; set; }

    public bool IsRequired { get; set; }

    public string? OptionsJson { get; set; }

    // ============================================================
    // VALIDATION
    // ============================================================

    public string? RegexPattern { get; set; }

    public string? RegexMessage { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public string? ValidationType { get; set; }

    // ============================================================
    // ATTACHMENT CONFIG
    // ============================================================

    public LibraryFieldAttachmentConfig? AttachmentConfig { get; set; }
}

public sealed class LibraryFieldAttachmentConfig : EntityBase
{
    public Guid LibraryFieldId { get; set; }

    public LibraryField LibraryField { get; set; } = null!;

    public string? AllowedFileTypesJson { get; set; }

    public int? MaxFileSizeMb { get; set; }

    public bool? AllowMultiple { get; set; }
}