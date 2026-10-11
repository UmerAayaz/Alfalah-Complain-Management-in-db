namespace BankingPlatform.Application.DTOs;

public sealed record FormDto(
    Guid Id,
    string FormKey,
    int Version,
    string Name,
    string? Description,
    string Status,
    DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<FormFieldItemDto> Fields);

public sealed record FormFieldItemDto(
    Guid Id,
    Guid LibraryFieldId,
    string FieldKey,
    string Label,
    string FieldType,
    bool IsRequired,
    int DisplayOrder,
    string? Placeholder,
    string? RegexPattern,
    string? RegexMessage,
    int? MinLength,
    int? MaxLength,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationType,
    IReadOnlyList<string> Options,
    IReadOnlyList<string>? AllowedFileTypes,
    int? MaxFileSizeMb,
    bool? AllowMultiple);

public sealed record FormListItemDto(
    Guid Id,
    string FormKey,
    int Version,
    string Name,
    string Status,
    DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc,
    int FieldCount);

    public sealed record FormFieldRequest(
    Guid LibraryFieldId,
    bool? IsRequiredOverride = null);

public sealed record CreateFormRequest(
    string Name,
    string? Description,
    IReadOnlyList<FormFieldRequest> Fields);
// public sealed record CreateFormRequest(
//     string Name,
//     string? Description,
//     IReadOnlyList<Guid> LibraryFieldIds);

public sealed record UpdateFormRequest(
    string Name,
    string? Description,
    IReadOnlyList<FormFieldRequest> Fields);