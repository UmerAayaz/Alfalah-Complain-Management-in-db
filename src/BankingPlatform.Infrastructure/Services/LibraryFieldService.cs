using System.Text.Json;
using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Services;

public sealed class LibraryFieldService(AppDbContext db) : ILibraryFieldService
{
    public async Task<IReadOnlyList<LibraryFieldDto>> ListAsync(CancellationToken ct)
    {
        var fields = await db.LibraryFields
            .AsNoTracking()
            .Include(x => x.AttachmentConfig)
            .OrderBy(x => x.Label)
            .ToListAsync(ct);

        return fields.Select(Map).ToList();
    }

    public async Task<LibraryFieldDto> GetAsync(Guid id, CancellationToken ct)
    {
        var field = await db.LibraryFields
            .AsNoTracking()
            .Include(x => x.AttachmentConfig)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Library field not found.");

        return Map(field);
    }

    public async Task<LibraryFieldDto> CreateAsync(
        CreateLibraryFieldRequest request,
        CancellationToken ct)
    {
        Validate(request);

        var key = request.FieldKey.Trim().ToLowerInvariant();

        var exists = await db.LibraryFields
            .AnyAsync(x => x.FieldKey == key, ct);

        if (exists)
            throw new ArgumentException($"Field key '{request.FieldKey}' already exists.");

        var field = new LibraryField
        {
            FieldKey = key,
            Label = request.Label.Trim(),
            FieldType = request.FieldType.Trim().ToLowerInvariant(),
            Placeholder = request.Placeholder?.Trim(),
            IsRequired = request.IsRequired,
            OptionsJson = request.Options is { Count: > 0 }
                ? JsonSerializer.Serialize(request.Options)
                : null,
            RegexPattern = NullIfEmpty(request.RegexPattern),
            RegexMessage = NullIfEmpty(request.RegexMessage),
            MinLength = request.MinLength,
            MaxLength = request.MaxLength,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            ValidationType = NullIfEmpty(request.ValidationType),
        };

        if (field.FieldType == "attachment")
        {
            field.AttachmentConfig = new LibraryFieldAttachmentConfig
            {
                AllowedFileTypesJson = request.AllowedFileTypes is { Count: > 0 }
                    ? JsonSerializer.Serialize(request.AllowedFileTypes)
                    : null,
                MaxFileSizeMb = request.MaxFileSizeMb ?? 5,
                AllowMultiple = request.AllowMultiple ?? false,
            };
        }

        db.LibraryFields.Add(field);
        await db.SaveChangesAsync(ct);

        return Map(field);
    }

    public async Task<LibraryFieldDto> UpdateAsync(
        Guid id,
        UpdateLibraryFieldRequest request,
        CancellationToken ct)
    {
        var field = await db.LibraryFields
            .Include(x => x.AttachmentConfig)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Library field not found.");

        field.Label = request.Label.Trim();
        field.Placeholder = request.Placeholder?.Trim();
        field.IsRequired = request.IsRequired;
        field.OptionsJson = request.Options is { Count: > 0 }
            ? JsonSerializer.Serialize(request.Options)
            : null;
        field.RegexPattern = NullIfEmpty(request.RegexPattern);
        field.RegexMessage = NullIfEmpty(request.RegexMessage);
        field.MinLength = request.MinLength;
        field.MaxLength = request.MaxLength;
        field.MinValue = request.MinValue;
        field.MaxValue = request.MaxValue;
        field.ValidationType = NullIfEmpty(request.ValidationType);
        field.UpdatedAtUtc = DateTime.UtcNow;

        if (field.FieldType == "attachment")
        {
            if (field.AttachmentConfig is null)
            {
                field.AttachmentConfig = new LibraryFieldAttachmentConfig
                {
                    LibraryFieldId = field.Id,
                };
            }

            field.AttachmentConfig.AllowedFileTypesJson =
                request.AllowedFileTypes is { Count: > 0 }
                    ? JsonSerializer.Serialize(request.AllowedFileTypes)
                    : null;
            field.AttachmentConfig.MaxFileSizeMb = request.MaxFileSizeMb ?? 5;
            field.AttachmentConfig.AllowMultiple = request.AllowMultiple ?? false;
        }

        await db.SaveChangesAsync(ct);
        return Map(field);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var field = await db.LibraryFields
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Library field not found.");

        db.LibraryFields.Remove(field);
        await db.SaveChangesAsync(ct);
    }

    // ============================================================
    // MAPPING + HELPERS
    // ============================================================

    private static LibraryFieldDto Map(LibraryField f) => new(
        f.Id,
        f.FieldKey,
        f.Label,
        f.FieldType,
        f.Placeholder,
        f.IsRequired,
        ParseOptions(f.OptionsJson),
        f.RegexPattern,
        f.RegexMessage,
        f.MinLength,
        f.MaxLength,
        f.MinValue,
        f.MaxValue,
        f.ValidationType,
        ParseAllowedTypes(f.AttachmentConfig?.AllowedFileTypesJson),
        f.AttachmentConfig?.MaxFileSizeMb,
        f.AttachmentConfig?.AllowMultiple);

    private static IReadOnlyList<string> ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return Array.Empty<string>(); }
    }

    private static IReadOnlyList<string>? ParseAllowedTypes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch { return null; }
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static void Validate(CreateLibraryFieldRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FieldKey))
            throw new ArgumentException("Field key is required.");

        if (string.IsNullOrWhiteSpace(request.Label))
            throw new ArgumentException("Label is required.");

        if (string.IsNullOrWhiteSpace(request.FieldType))
            throw new ArgumentException("Field type is required.");

        if (!string.IsNullOrWhiteSpace(request.RegexPattern))
        {
            try { _ = new System.Text.RegularExpressions.Regex(request.RegexPattern); }
            catch (ArgumentException)
            {
                throw new ArgumentException("Invalid regex pattern.");
            }
        }
    }
}