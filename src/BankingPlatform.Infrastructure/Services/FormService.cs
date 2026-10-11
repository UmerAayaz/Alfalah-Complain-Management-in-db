using System.Text.Json;
using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Services;

public sealed class FormService(
    AppDbContext db,
    ICurrentUser currentUser) : IFormService
{
    // ============================================================
    // LIST
    // ============================================================
    public async Task<IReadOnlyList<FormListItemDto>> ListAsync(CancellationToken ct)
    {
        var forms = await db.Forms
            .AsNoTracking()
            .Include(x => x.Fields)
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.Version)
            .ToListAsync(ct);

        return forms
            .GroupBy(x => x.FormKey)
            .Select(g => g.First())
            .Select(f => new FormListItemDto(
                f.Id, f.FormKey, f.Version, f.Name, f.Status,
                f.PublishedAtUtc, f.CreatedAtUtc, f.Fields.Count))
            .ToList();
    }

    // ============================================================
    // GET
    // ============================================================
    public async Task<FormDto> GetAsync(Guid id, CancellationToken ct)
    {
        var form = await db.Forms
            .AsNoTracking()
            .Include(x => x.Fields)
                .ThenInclude(x => x.LibraryField)
                    .ThenInclude(lf => lf.AttachmentConfig)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Form not found.");

        return MapForm(form);
    }

    // ============================================================
    // CREATE
    // ============================================================
    public async Task<FormDto> CreateAsync(CreateFormRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Form name is required.");

        if (request.Fields is null || request.Fields.Count == 0)
            throw new ArgumentException("At least one field is required.");

        var formKey = GenerateFormKey(request.Name);

        var existingVersions = await db.Forms
            .Where(x => x.FormKey == formKey)
            .ToListAsync(ct);

        var nextVersion = existingVersions.Count == 0
            ? 1
            : existingVersions.Max(x => x.Version) + 1;

        var form = new Form
        {
            FormKey = formKey,
            Version = nextVersion,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Status = "Draft",
            CreatedByUserId = currentUser.UserId,
        };

        // Validate library fields exist
        var requestedFieldIds = request.Fields.Select(x => x.LibraryFieldId).ToList();

        var libraryFields = await db.LibraryFields
            .Where(x => requestedFieldIds.Contains(x.Id))
            .ToListAsync(ct);

        if (libraryFields.Count != requestedFieldIds.Count)
            throw new ArgumentException("One or more library fields not found.");

        for (int i = 0; i < request.Fields.Count; i++)
        {
            var req = request.Fields[i];
            form.Fields.Add(new FormField
            {
                LibraryFieldId = req.LibraryFieldId,
                DisplayOrder = i,
                IsRequiredOverride = req.IsRequiredOverride,
            });
        }

        db.Forms.Add(form);
        await db.SaveChangesAsync(ct);

        return await GetAsync(form.Id, ct);
    }

    // ============================================================
    // UPDATE — DIFF-BASED
    // ============================================================
    // public async Task<FormDto> UpdateAsync(Guid id, UpdateFormRequest request, CancellationToken ct)
    // {
    //     var form = await db.Forms
    //         .Include(x => x.Fields)
    //         .FirstOrDefaultAsync(x => x.Id == id, ct)
    //         ?? throw new KeyNotFoundException("Form not found.");

    //     if (form.Status == "Published")
    //         throw new InvalidOperationException("Published forms are immutable. Create a new version.");

    //     if (string.IsNullOrWhiteSpace(request.Name))
    //         throw new ArgumentException("Form name is required.");

    //     if (request.Fields is null || request.Fields.Count == 0)
    //         throw new ArgumentException("At least one field is required.");

    //     // Validate library fields exist
    //     var requestedFieldIds = request.Fields.Select(x => x.LibraryFieldId).ToList();

    //     var libraryFields = await db.LibraryFields
    //         .Where(x => requestedFieldIds.Contains(x.Id))
    //         .ToListAsync(ct);

    //     if (libraryFields.Count != requestedFieldIds.Count)
    //         throw new ArgumentException("One or more library fields not found.");

    //     // ---- Update metadata ----
    //     form.Name = request.Name.Trim();
    //     form.Description = request.Description?.Trim();
    //     form.UpdatedAtUtc = DateTime.UtcNow;

    //     // ============================================================
    //     // DIFF-BASED UPDATE
    //     // ============================================================

    //     var requestedIds = requestedFieldIds.ToHashSet();
    //     var existingByLibraryId = form.Fields
    //         .ToDictionary(x => x.LibraryFieldId);

    //     // 1. DELETE — fields removed
    //     foreach (var field in form.Fields.ToList())
    //     {
    //         if (!requestedIds.Contains(field.LibraryFieldId))
    //         {
    //             db.FormFields.Remove(field);
    //             form.Fields.Remove(field);
    //         }
    //     }

    //     // 2. UPDATE — existing fields: refresh DisplayOrder + IsRequiredOverride
    //     for (int i = 0; i < request.Fields.Count; i++)
    //     {
    //         var req = request.Fields[i];

    //         if (existingByLibraryId.TryGetValue(req.LibraryFieldId, out var existing))
    //         {
    //             existing.DisplayOrder = i;
    //             existing.IsRequiredOverride = req.IsRequiredOverride;
    //             existing.UpdatedAtUtc = DateTime.UtcNow;
    //         }
    //     }

    //     // 3. INSERT — new fields
    //     for (int i = 0; i < request.Fields.Count; i++)
    //     {
    //         var req = request.Fields[i];

    //         if (!existingByLibraryId.ContainsKey(req.LibraryFieldId))
    //         {
    //             form.Fields.Add(new FormField
    //             {
    //                 FormId = form.Id,
    //                 LibraryFieldId = req.LibraryFieldId,
    //                 DisplayOrder = i,
    //                 IsRequiredOverride = req.IsRequiredOverride,
    //             });
    //         }
    //     }

    //     await db.SaveChangesAsync(ct);
    //     return await GetAsync(id, ct);
    // }

    // ============================================================
    // DELETE
    // ============================================================
    
        public async Task<FormDto> UpdateAsync(Guid id, UpdateFormRequest request, CancellationToken ct)
    {
        // ---- 1. Load form WITHOUT tracking (no Include Fields) ----
        var form = await db.Forms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Form not found.");

        if (form.Status == "Published")
            throw new InvalidOperationException("Published forms are immutable. Create a new version.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Form name is required.");

        if (request.Fields is null || request.Fields.Count == 0)
            throw new ArgumentException("At least one field is required.");

        // ---- 2. Validate library fields exist ----
        var requestedFieldIds = request.Fields.Select(x => x.LibraryFieldId).ToList();

        var libraryFields = await db.LibraryFields
            .AsNoTracking()
            .Where(x => requestedFieldIds.Contains(x.Id))
            .ToListAsync(ct);

        if (libraryFields.Count != requestedFieldIds.Count)
            throw new ArgumentException("One or more library fields not found.");

        // ---- 3. Clear any tracked entities from previous operations ----
        db.ChangeTracker.Clear();

        // ---- 4. Update form metadata via raw SQL (no EF tracking) ----
        var now = DateTime.UtcNow;
        var trimmedName = request.Name.Trim();
        var trimmedDesc = request.Description?.Trim();

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Forms
            SET Name = {trimmedName},
                Description = {trimmedDesc},
                UpdatedAtUtc = {now}
            WHERE Id = {id}", ct);

        // ---- 5. Delete ALL existing fields via raw SQL (bypasses EF) ----
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM FormFields WHERE FormId = {id}", ct);

        // ---- 6. Insert new fields (EF only INSERTs) ----
        for (int i = 0; i < request.Fields.Count; i++)
        {
            var req = request.Fields[i];
            db.FormFields.Add(new FormField
            {
                FormId = id,
                LibraryFieldId = req.LibraryFieldId,
                DisplayOrder = i,
                IsRequiredOverride = req.IsRequiredOverride,
            });
        }

        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }
    
    
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var form = await db.Forms
            .Include(x => x.Fields)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Form not found.");

        db.Forms.Remove(form);
        await db.SaveChangesAsync(ct);
    }

    // ============================================================
    // PUBLISH
    // ============================================================
    public async Task<FormDto> PublishAsync(Guid id, CancellationToken ct)
    {
        var form = await db.Forms
            .Include(x => x.Fields)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Form not found.");

        if (form.Status == "Published")
            throw new InvalidOperationException("Form is already published.");

        if (form.Fields.Count == 0)
            throw new InvalidOperationException("Cannot publish an empty form.");

        var oldPublished = await db.Forms
            .Where(x => x.FormKey == form.FormKey && x.Status == "Published")
            .ToListAsync(ct);

        foreach (var old in oldPublished)
        {
            old.Status = "Archived";
            old.UpdatedAtUtc = DateTime.UtcNow;
        }

        form.Status = "Published";
        form.PublishedAtUtc = DateTime.UtcNow;
        form.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    // ============================================================
    // CREATE NEW VERSION
    // ============================================================
    public async Task<FormDto> CreateNewVersionAsync(Guid id, CancellationToken ct)
    {
        var source = await db.Forms
            .AsNoTracking()
            .Include(x => x.Fields)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Form not found.");

        var nextVersion = (await db.Forms
            .Where(x => x.FormKey == source.FormKey)
            .MaxAsync(x => (int?)x.Version, ct) ?? 0) + 1;

        var newForm = new Form
        {
            FormKey = source.FormKey,
            Version = nextVersion,
            Name = source.Name,
            Description = source.Description,
            Status = "Draft",
            CreatedByUserId = currentUser.UserId,
        };

        foreach (var field in source.Fields.OrderBy(x => x.DisplayOrder))
        {
            newForm.Fields.Add(new FormField
            {
                LibraryFieldId = field.LibraryFieldId,
                DisplayOrder = field.DisplayOrder,
                LabelOverride = field.LabelOverride,
                IsRequiredOverride = field.IsRequiredOverride,
            });
        }

        db.Forms.Add(newForm);
        await db.SaveChangesAsync(ct);

        return await GetAsync(newForm.Id, ct);
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private static string GenerateFormKey(string name)
    {
        var chars = name.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray();
        var key = new string(chars);
        while (key.Contains("__")) key = key.Replace("__", "_");
        return key.Trim('_');
    }

    private static FormDto MapForm(Form f) => new(
        f.Id, f.FormKey, f.Version, f.Name, f.Description, f.Status,
        f.PublishedAtUtc, f.CreatedAtUtc,
        f.Fields
            .OrderBy(x => x.DisplayOrder)
            .Select(MapField)
            .ToList());

    private static FormFieldItemDto MapField(FormField ff)
    {
        var lf = ff.LibraryField;

        return new FormFieldItemDto(
            ff.Id,
            ff.LibraryFieldId,
            lf.FieldKey,
            ff.LabelOverride ?? lf.Label,
            lf.FieldType,
            ff.IsRequiredOverride ?? lf.IsRequired,
            ff.DisplayOrder,
            lf.Placeholder,
            lf.RegexPattern,
            lf.RegexMessage,
            lf.MinLength,
            lf.MaxLength,
            lf.MinValue,
            lf.MaxValue,
            lf.ValidationType,
            ParseOptions(lf.OptionsJson),
            ParseAllowedTypes(lf.AttachmentConfig?.AllowedFileTypesJson),
            lf.AttachmentConfig?.MaxFileSizeMb,
            lf.AttachmentConfig?.AllowMultiple);
    }

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
}