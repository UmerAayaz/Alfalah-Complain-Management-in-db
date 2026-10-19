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
            DesignerJson = JsonSerializer.Serialize(request),
            DefaultPriority = request.DefaultPriority,
            ProductId = request.ProductId
        };

        BuildGraph(definition, request);
        db.WorkflowDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        var dto = Map(definition);
        db.ChangeTracker.Clear();
        return dto;
    }

    // ============================================================
    // UPDATE DRAFT
    // ============================================================

    public async Task<WorkflowDefinitionDto> UpdateDraftAsync(
        Guid id,
        CreateWorkflowDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        var existing = await db.WorkflowDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        await EnsureDepartmentAdminAsync(existing.DepartmentId, cancellationToken);

        if (existing.Status != WorkflowDefinitionStatus.Draft)
            throw new InvalidOperationException(
                "Published workflows are immutable. Create a new version instead.");

        db.ChangeTracker.Clear();

        // STEP 1: Purane children delete
        // await db.Database.ExecuteSqlInterpolatedAsync($@"
        //     DELETE FROM WorkflowNodeFieldAttachmentConfigs
        //     WHERE WorkflowNodeFieldId IN (
        //         SELECT f.Id FROM WorkflowNodeFields f
        //         INNER JOIN WorkflowNodes n ON n.Id = f.WorkflowNodeId
        //         WHERE n.WorkflowDefinitionId = {id}
        //     )", cancellationToken);

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

        // STEP 2: Parent update
        var designerJson = JsonSerializer.Serialize(request);
        var now = DateTime.UtcNow;

await db.Database.ExecuteSqlInterpolatedAsync($@"
    UPDATE WorkflowDefinitions
    SET Name = {request.Name.Trim()},
        DepartmentId = {request.DepartmentId},
        CategoryId = {request.CategoryId},
        DesignerJson = {designerJson},
        DefaultPriority = {request.DefaultPriority},
        ProductId = {request.ProductId},
        UpdatedAtUtc = {now}
    WHERE Id = {id}", cancellationToken);

        // STEP 3: Fresh parent (tracked)
        db.ChangeTracker.Clear();
        var fresh = await db.WorkflowDefinitions
            .FirstAsync(x => x.Id == id, cancellationToken);

        // STEP 4: BuildGraph — new nodes/transitions
        BuildGraph(fresh, request);

        // STEP 5: EXPLICIT tracking — EF ko pakka batao
        foreach (var node in fresh.Nodes)
        {
            db.WorkflowNodes.Add(node);

            foreach (var field in node.Fields)
            {
                db.WorkflowNodeFields.Add(field);

                // if (field.AttachmentConfig != null)
                //     db.WorkflowNodeFieldAttachmentConfigs.Add(field.AttachmentConfig);
            }
        }

        foreach (var transition in fresh.Transitions)
        {
            db.WorkflowTransitions.Add(transition);
        }

        // STEP 6: Save
        await db.SaveChangesAsync(cancellationToken);

        // STEP 7: DTO + clear tracker
        var dto = Map(fresh);
        db.ChangeTracker.Clear();
        return dto;
    }

    // ============================================================
    // PUBLISH
    // ============================================================

    public async Task<WorkflowDefinitionDto> PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();

        var definition = await db.WorkflowDefinitions
            .Include(x => x.Nodes)
            .Include(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        await EnsureDepartmentAdminAsync(definition.DepartmentId, cancellationToken);

        if (definition.Status != WorkflowDefinitionStatus.Draft)
            throw new InvalidOperationException("Only draft workflows can be published.");

        // Validation
        var startCount = definition.Nodes.Count(x => x.Type == WorkflowNodeType.Start);
        if (startCount != 1)
            throw new InvalidOperationException(
                $"Cannot publish: workflow must have exactly 1 Start node (found {startCount}).");

        var endCount = definition.Nodes.Count(x => x.Type == WorkflowNodeType.End);
        if (endCount < 1)
            throw new InvalidOperationException(
                "Cannot publish: workflow must have at least 1 End node.");

        if (definition.Nodes.Count < 2)
            throw new InvalidOperationException(
                "Cannot publish: workflow must have at least Start and End nodes.");

        var humanCount = definition.Nodes.Count(x => x.Type == WorkflowNodeType.HumanTask);
        if (humanCount < 1)
            throw new InvalidOperationException(
                "Cannot publish: workflow must have at least 1 Human Task between Start and End nodes.");

        var nodeIds = definition.Nodes.Select(x => x.Id).ToHashSet();
        foreach (var t in definition.Transitions)
        {
            if (!nodeIds.Contains(t.SourceNodeId))
                throw new InvalidOperationException(
                    "Cannot publish: a transition references a missing source node.");
            if (!nodeIds.Contains(t.TargetNodeId))
                throw new InvalidOperationException(
                    "Cannot publish: a transition references a missing target node.");
        }

        var attachedFormIds = definition.Nodes
            .Where(x => x.FormId.HasValue)
            .Select(x => x.FormId!.Value)
            .Distinct()
            .ToList();

        if (attachedFormIds.Count > 0)
        {
            var unpublishedCount = await db.Forms
                .Where(f => attachedFormIds.Contains(f.Id) && f.Status != "Published")
                .CountAsync(cancellationToken);

            if (unpublishedCount > 0)
                throw new InvalidOperationException(
                    $"Cannot publish: {unpublishedCount} attached form(s) are not Published.");
        }

        var startNode = definition.Nodes.First(x => x.Type == WorkflowNodeType.Start);
        var visited = new HashSet<Guid> { startNode.Id };
        var queue = new Queue<Guid>();
        queue.Enqueue(startNode.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var outgoing = definition.Transitions
                .Where(t => t.SourceNodeId == current)
                .Select(t => t.TargetNodeId);
            foreach (var next in outgoing)
                if (visited.Add(next))
                    queue.Enqueue(next);
        }

        var unreachable = definition.Nodes
            .Where(n => !visited.Contains(n.Id))
            .Select(n => n.Name)
            .ToList();

        if (unreachable.Count > 0)
            throw new InvalidOperationException(
                "Cannot publish: unreachable nodes — " + string.Join(", ", unreachable));

        // Archive + Publish
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

        var dto = Map(definition);
        db.ChangeTracker.Clear();
        return dto;
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

    private async Task EnsureDepartmentAdminAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var allowed = await db.DepartmentMemberships
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == currentUser.UserId &&
                x.DepartmentId == departmentId &&
                (x.RoleCode == "DeptAdmin" || x.RoleCode == "UnitHead"),
                cancellationToken);

        if (!allowed)
            throw new UnauthorizedAccessException(
                $"Workflow access denied. UserId={currentUser.UserId}, DepartmentId={departmentId}");
    }

    private static WorkflowDefinitionDto Map(WorkflowDefinition x) =>
        new(x.Id, x.Name, x.DepartmentId, x.CategoryId, x.Version, x.Status,
            x.DesignerJson, x.PublishedAtUtc, x.DefaultPriority, x.ProductId, null);

    private static void BuildGraph(WorkflowDefinition definition, CreateWorkflowDefinitionRequest request)
    {
        var byKey = new Dictionary<string, WorkflowNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Nodes)
        {
            var node = new WorkflowNode
            {
                WorkflowDefinitionId = definition.Id,
                NodeKey = item.Key.Trim(),
                Name = item.Name.Trim(),
                Type = item.Type,
                RoleCode = item.RoleCode?.Trim(),
                FormId = item.FormId,
                SlaHours = item.SlaHours,
                EscalationRoleCode = item.EscalationRoleCode?.Trim(),
                PositionX = item.X,
                PositionY = item.Y,
                ConfigJson = item.ConfigJson
            };
if (item.Type == WorkflowNodeType.HumanTask &&
    item.Fields is not null)
{
    foreach (var field in item.Fields.OrderBy(x => x.DisplayOrder))
    {
        var fieldType = field.FieldType.Trim().ToLowerInvariant();

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

        node.Fields.Add(nodeField);
    }
}
            definition.Nodes.Add(node);
            byKey[node.NodeKey] = node;
        }

        foreach (var edge in request.Edges)
        {
            if (!byKey.TryGetValue(edge.SourceKey.Trim(), out var sourceNode))
                throw new ArgumentException($"Source node '{edge.SourceKey}' was not found.");

            if (!byKey.TryGetValue(edge.TargetKey.Trim(), out var targetNode))
                throw new ArgumentException($"Target node '{edge.TargetKey}' was not found.");

            definition.Transitions.Add(new WorkflowTransition
            {
                WorkflowDefinitionId = definition.Id,
                SourceNodeId = sourceNode.Id,
                TargetNodeId = targetNode.Id,
                OutcomeKey = string.IsNullOrWhiteSpace(edge.OutcomeKey) ? null : edge.OutcomeKey.Trim(),
                Label = string.IsNullOrWhiteSpace(edge.Label) ? null : edge.Label.Trim()
            });
        }
    }

    public sealed class WorkflowNodeConfig
    {
        public string AssignmentMode { get; set; } = "queue";
        public bool SendEmailNotification { get; set; }
    }

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

        var humanCount = request.Nodes.Count(x => x.Type == WorkflowNodeType.HumanTask);
        if (humanCount < 1)
            throw new ArgumentException("Workflow must have at least 1 Human Task between Start and End nodes.");

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
                if (visited.Add(n))
                    q.Enqueue(n);
        }

        var unreachable = keys.Where(k => !visited.Contains(k)).ToList();
        if (unreachable.Count > 0)
            throw new ArgumentException("Unreachable nodes: " + string.Join(", ", unreachable));
    }
}