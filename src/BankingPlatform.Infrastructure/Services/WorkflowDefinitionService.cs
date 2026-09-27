using System.Text.Json;
using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Services;

public sealed class WorkflowDefinitionService(AppDbContext db, ICurrentUser currentUser) : IWorkflowDefinitionService
{
    public async Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        await EnsureDepartmentAdminAsync(request.DepartmentId, cancellationToken);

        var category = await db.ComplaintCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId && x.DepartmentId == request.DepartmentId, cancellationToken)
            ?? throw new InvalidOperationException("Category does not belong to the selected department.");

        var nextVersion = (await db.WorkflowDefinitions
            .Where(x => x.CategoryId == request.CategoryId)
            .MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;

        var definition = new WorkflowDefinition
        {
            Name = request.Name.Trim(),
            DepartmentId = request.DepartmentId,
            CategoryId = request.CategoryId,
            Version = nextVersion,
            Status = WorkflowDefinitionStatus.Draft,
            CreatedByUserId = currentUser.UserId,
            DesignerJson = JsonSerializer.Serialize(request)
        };

        BuildGraph(definition, request);
        db.WorkflowDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        return Map(definition);
    }

    // ============================================================
    // UPDATE DRAFT — pure raw SQL, no EF UPDATE/DELETE
    // ============================================================

    public async Task<WorkflowDefinitionDto> UpdateDraftAsync(
        Guid id,
        CreateWorkflowDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        // ---- 1. Load fresh WITHOUT tracking ----
        var existing = await db.WorkflowDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        await EnsureDepartmentAdminAsync(existing.DepartmentId, cancellationToken);

        if (request.DepartmentId != existing.DepartmentId ||
            request.CategoryId != existing.CategoryId)
            throw new InvalidOperationException(
                "A draft version cannot be moved to another department/category. Create a new workflow there instead.");

        if (existing.Status != WorkflowDefinitionStatus.Draft)
            throw new InvalidOperationException(
                "Published workflows are immutable. Create a new version instead.");

        // ---- 2. Clear any tracked entities ----
        db.ChangeTracker.Clear();

        // ---- 3. Delete all children via raw SQL (deepest first) ----
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM WorkflowNodeFieldAttachmentConfigs
            WHERE WorkflowNodeFieldId IN (
                SELECT f.Id
                FROM WorkflowNodeFields f
                INNER JOIN WorkflowNodes n ON n.Id = f.WorkflowNodeId
                WHERE n.WorkflowDefinitionId = {id}
            )", cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM WorkflowNodeFields
            WHERE WorkflowNodeId IN (
                SELECT Id FROM WorkflowNodes WHERE WorkflowDefinitionId = {id}
            )", cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM WorkflowTransitions
            WHERE WorkflowDefinitionId = {id}", cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM WorkflowNodes
            WHERE WorkflowDefinitionId = {id}", cancellationToken);

        // ---- 4. Update parent row via raw SQL (no EF UPDATE) ----
        var designerJson = JsonSerializer.Serialize(request);
        var now = DateTime.UtcNow;

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE WorkflowDefinitions
            SET Name = {request.Name.Trim()},
                DepartmentId = {request.DepartmentId},
                CategoryId = {request.CategoryId},
                DesignerJson = {designerJson},
                UpdatedAtUtc = {now}
            WHERE Id = {id}", cancellationToken);

        // ---- 5. Reload parent fresh (no tracking) ----
        db.ChangeTracker.Clear();

        var fresh = await db.WorkflowDefinitions
            .AsNoTracking()
            .FirstAsync(x => x.Id == id, cancellationToken);

        // Attach as Unchanged so EF won't try to UPDATE it
        db.WorkflowDefinitions.Attach(fresh);
        db.Entry(fresh).State = EntityState.Unchanged;

        // Ensure nav collections are empty (they are, since no-tracking)
        fresh.Nodes.Clear();
        fresh.Transitions.Clear();

        // ---- 6. Build new graph (only INSERTs now) ----
        BuildGraph(fresh, request);

        // ---- 7. Save — EF only INSERTs new rows ----
        await db.SaveChangesAsync(cancellationToken);

        return Map(fresh);
    }

    public async Task<WorkflowDefinitionDto> PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await db.WorkflowDefinitions
            .Include(x => x.Nodes)
            .Include(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        await EnsureDepartmentAdminAsync(definition.DepartmentId, cancellationToken);

        if (definition.Status != WorkflowDefinitionStatus.Draft)
            throw new InvalidOperationException("Only draft workflows can be published.");

        var oldPublished = await db.WorkflowDefinitions
            .Where(x => x.CategoryId == definition.CategoryId && x.Status == WorkflowDefinitionStatus.Published)
            .ToListAsync(cancellationToken);

        foreach (var old in oldPublished)
        {
            old.Status = WorkflowDefinitionStatus.Archived;
            old.UpdatedAtUtc = DateTime.UtcNow;
        }

        definition.Status = WorkflowDefinitionStatus.Published;
        definition.PublishedAtUtc = DateTime.UtcNow;
        definition.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(definition);
    }

    public async Task<WorkflowDefinitionDto> CreateNewVersionAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await db.WorkflowDefinitions.AsNoTracking()
            .Include(x => x.Nodes)
            .Include(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        await EnsureDepartmentAdminAsync(source.DepartmentId, cancellationToken);

        var request = JsonSerializer.Deserialize<CreateWorkflowDefinitionRequest>(source.DesignerJson)
            ?? throw new InvalidOperationException("Stored designer JSON is invalid.");

        return await CreateAsync(request with { Name = source.Name }, cancellationToken);
    }

    public async Task<WorkflowDefinitionDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var x = await db.WorkflowDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return x is null ? null : Map(x);
    }

    public async Task<IReadOnlyList<WorkflowDefinitionDto>> ListAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = db.WorkflowDefinitions.AsNoTracking().AsQueryable();
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);

        var items = await query
            .OrderBy(x => x.CategoryId)
            .ThenByDescending(x => x.Version)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    // ============================================================
    // AUTHORIZATION
    // ============================================================

    private async Task EnsureDepartmentAdminAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var allowed = await db.DepartmentMemberships
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == currentUser.UserId &&
                x.DepartmentId == departmentId &&
                (x.RoleCode == "DeptAdmin" || x.RoleCode == "UnitHead"),
                cancellationToken);

        if (!allowed)
        {
            throw new UnauthorizedAccessException(
                $"Workflow access denied. UserId={currentUser.UserId}, DepartmentId={departmentId}");
        }
    }

    private static WorkflowDefinitionDto Map(WorkflowDefinition x) =>
        new(x.Id, x.Name, x.DepartmentId, x.CategoryId, x.Version, x.Status, x.DesignerJson, x.PublishedAtUtc);

    // ============================================================
    // BUILD GRAPH (nodes + fields + transitions)
    // ============================================================

    private static void BuildGraph(
        WorkflowDefinition definition,
        CreateWorkflowDefinitionRequest request)
    {
        var byKey = new Dictionary<string, WorkflowNode>(
            StringComparer.OrdinalIgnoreCase);

        // ---------------------------------------------------------
        // 1. CREATE WORKFLOW NODES + DYNAMIC FIELDS
        // ---------------------------------------------------------
        foreach (var item in request.Nodes)
        {
            var node = new WorkflowNode
            {
                WorkflowDefinitionId = definition.Id,
                NodeKey = item.Key.Trim(),
                Name = item.Name.Trim(),
                Type = item.Type,
                RoleCode = item.RoleCode?.Trim(),
                SlaHours = item.SlaHours,
                EscalationRoleCode = item.EscalationRoleCode?.Trim(),
                PositionX = item.X,
                PositionY = item.Y,
                ConfigJson = item.ConfigJson
            };

            // Dynamic fields only belong to Human Task nodes
            if (item.Type == WorkflowNodeType.HumanTask &&
                item.Fields is not null)
            {
                foreach (var field in item.Fields.OrderBy(x => x.DisplayOrder))
                {
                    var fieldType = field.FieldType.Trim().ToLowerInvariant();
                    var isAttachment = fieldType == "attachment";

                    var nodeField = new WorkflowNodeField
                    {
                        WorkflowNodeId = node.Id,
                        FieldKey = field.FieldKey.Trim(),
                        Label = field.Label.Trim(),
                        FieldType = fieldType,
                        Placeholder = field.Placeholder?.Trim(),
                        IsRequired = field.IsRequired,
                        DisplayOrder = field.DisplayOrder,
                        OptionsJson = field.Options is null
                            ? null
                            : JsonSerializer.Serialize(field.Options),
                    };

                    // Attach the 1:1 config row for attachment fields only
                    if (isAttachment)
                    {
                        nodeField.AttachmentConfig = new WorkflowNodeFieldAttachmentConfig
                        {
                            AllowedFileTypesJson = field.AllowedFileTypes is { Count: > 0 }
                                ? JsonSerializer.Serialize(field.AllowedFileTypes)
                                : null,
                            MaxFileSizeMb = field.MaxFileSizeMb,
                            AllowMultiple = field.AllowMultiple,
                        };
                    }

                    node.Fields.Add(nodeField);
                }
            }

            definition.Nodes.Add(node);

            byKey[node.NodeKey] = node;
        }

        // ---------------------------------------------------------
        // 2. VALIDATE HUMAN TASKS + THEIR DYNAMIC FIELDS
        // ---------------------------------------------------------
        foreach (var node in request.Nodes.Where(
                     x => x.Type == WorkflowNodeType.HumanTask))
        {
            if (string.IsNullOrWhiteSpace(node.RoleCode))
            {
                throw new ArgumentException(
                    $"Human task '{node.Name}' needs a roleCode.");
            }

            if (node.SlaHours is <= 0)
            {
                throw new ArgumentException(
                    $"SLA hours for '{node.Name}' must be positive.");
            }

            var fields =
                node.Fields ??
                Array.Empty<WorkflowNodeFieldRequest>();

            var keys = fields
                .Select(x => x.FieldKey.Trim())
                .ToList();

            if (keys.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(
                    $"Every field in '{node.Name}' needs a field key.");
            }

            if (keys.Count !=
                keys.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                throw new ArgumentException(
                    $"Duplicate field keys found in '{node.Name}'.");
            }

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Label))
                {
                    throw new ArgumentException(
                        $"Every field in '{node.Name}' needs a label.");
                }

                if (string.IsNullOrWhiteSpace(field.FieldType))
                {
                    throw new ArgumentException(
                        $"Field '{field.Label}' needs a field type.");
                }
            }
        }

        // ---------------------------------------------------------
        // 3. CREATE WORKFLOW TRANSITIONS FROM DESIGNER EDGES
        // ---------------------------------------------------------
        foreach (var edge in request.Edges)
        {
            if (!byKey.TryGetValue(edge.SourceKey.Trim(), out var sourceNode))
            {
                throw new ArgumentException(
                    $"Source node '{edge.SourceKey}' was not found.");
            }

            if (!byKey.TryGetValue(edge.TargetKey.Trim(), out var targetNode))
            {
                throw new ArgumentException(
                    $"Target node '{edge.TargetKey}' was not found.");
            }

            var transition = new WorkflowTransition
            {
                WorkflowDefinitionId = definition.Id,
                SourceNodeId = sourceNode.Id,
                TargetNodeId = targetNode.Id,
                OutcomeKey = string.IsNullOrWhiteSpace(edge.OutcomeKey)
                    ? null
                    : edge.OutcomeKey.Trim(),
                Label = string.IsNullOrWhiteSpace(edge.Label)
                    ? null
                    : edge.Label.Trim()
            };

            definition.Transitions.Add(transition);
        }
    }

    public sealed class WorkflowNodeConfig
    {
        public string AssignmentMode { get; set; } = "queue";
        public bool SendEmailNotification { get; set; }
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void Validate(CreateWorkflowDefinitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Workflow name is required.");

        if (request.Nodes.Count == 0)
            throw new ArgumentException("Workflow needs at least one node.");

        if (request.Nodes.Count(x => x.Type == WorkflowNodeType.Start) != 1)
            throw new ArgumentException("Workflow must contain exactly one Start node.");

        if (request.Nodes.All(x => x.Type != WorkflowNodeType.End))
            throw new ArgumentException("Workflow needs at least one End node.");

        var keys = request.Nodes.Select(x => x.Key.Trim()).ToList();
        if (keys.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Every node needs a key.");

        if (keys.Count != keys.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new ArgumentException("Node keys must be unique.");

        var keySet = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in request.Edges)
        {
            if (!keySet.Contains(edge.SourceKey) || !keySet.Contains(edge.TargetKey))
                throw new ArgumentException($"Edge {edge.SourceKey} -> {edge.TargetKey} references a missing node.");
        }

        foreach (var node in request.Nodes.Where(x => x.Type == WorkflowNodeType.HumanTask))
        {
            if (string.IsNullOrWhiteSpace(node.RoleCode))
                throw new ArgumentException($"Human task '{node.Name}' needs a roleCode.");

            if (node.SlaHours is <= 0)
                throw new ArgumentException($"SLA hours for '{node.Name}' must be positive.");
        }

        var start = request.Nodes.Single(x => x.Type == WorkflowNodeType.Start).Key;
        var adjacency = request.Edges
            .GroupBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(e => e.TargetKey).ToList(), StringComparer.OrdinalIgnoreCase);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var q = new Queue<string>();
        q.Enqueue(start);
        visited.Add(start);

        while (q.Count > 0)
        {
            var current = q.Dequeue();
            if (!adjacency.TryGetValue(current, out var next)) continue;

            foreach (var n in next)
                if (visited.Add(n)) q.Enqueue(n);
        }

        var unreachable = keys.Where(k => !visited.Contains(k)).ToList();
        if (unreachable.Count > 0)
            throw new ArgumentException("Unreachable nodes: " + string.Join(", ", unreachable));
    }
}