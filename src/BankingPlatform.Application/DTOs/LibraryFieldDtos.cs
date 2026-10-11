namespace BankingPlatform.Application.DTOs;

public sealed record LibraryFieldDto(
    Guid Id,
    string FieldKey,
    string Label,
    string FieldType,
    string? Placeholder,
    bool IsRequired,
    IReadOnlyList<string> Options,
    string? RegexPattern,
    string? RegexMessage,
    int? MinLength,
    int? MaxLength,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationType,
    IReadOnlyList<string>? AllowedFileTypes,
    int? MaxFileSizeMb,
    bool? AllowMultiple);

public sealed record CreateLibraryFieldRequest(
    string FieldKey,
    string Label,
    string FieldType,
    string? Placeholder,
    bool IsRequired,
    IReadOnlyList<string>? Options,
    string? RegexPattern,
    string? RegexMessage,
    int? MinLength,
    int? MaxLength,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationType,
    IReadOnlyList<string>? AllowedFileTypes,
    int? MaxFileSizeMb,
    bool? AllowMultiple);

public sealed record UpdateLibraryFieldRequest(
    string Label,
    string? Placeholder,
    bool IsRequired,
    IReadOnlyList<string>? Options,
    string? RegexPattern,
    string? RegexMessage,
    int? MinLength,
    int? MaxLength,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationType,
    IReadOnlyList<string>? AllowedFileTypes,
    int? MaxFileSizeMb,
    bool? AllowMultiple);

public sealed record RegexPresetDto(
    string Key,
    string Label,
    string Pattern,
    string Message);                    