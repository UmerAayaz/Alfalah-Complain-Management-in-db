// ============================================================
// MIGRATE FORM → creates new workflow version with form updated
// ============================================================
public sealed record MigrateFormRequest(Guid OldFormId, Guid NewFormId);

public sealed record MigrateFormResult(
    int UpdatedCount,
    IReadOnlyList<string> UpdatedWorkflowNames,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Warnings);