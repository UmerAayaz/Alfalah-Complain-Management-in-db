using BankingPlatform.Application.DTOs;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankingPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/reference")]
public sealed class ReferenceController(AppDbContext db) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemDto>>> Departments(CancellationToken cancellationToken)
        => Ok(await db.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken));

[HttpGet("categories")]
public async Task<IActionResult> GetCategories(
    [FromQuery] Guid? departmentId,
    [FromServices] AppDbContext db,
    CancellationToken cancellationToken)
{
    var query = db.ComplaintCategories
        .Where(x => x.IsActive);   // ← ParentId hata diya

    if (departmentId.HasValue)
        query = query.Where(x => x.DepartmentId == departmentId.Value);

    var items = await query
        .OrderBy(x => x.Name)
        .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Name))
        .ToListAsync(cancellationToken);

    return Ok(items);
}

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserReferenceDto>>> Users(
        [FromQuery] Guid departmentId,
        [FromQuery] string? roleCode,
        CancellationToken cancellationToken)
    {
        var q = db.DepartmentMemberships.AsNoTracking()
            .Where(x => x.DepartmentId == departmentId && x.User.IsActive);
        if (!string.IsNullOrWhiteSpace(roleCode)) q = q.Where(x => x.RoleCode == roleCode);
        return Ok(await q.OrderBy(x => x.User.DisplayName)
            .Select(x => new UserReferenceDto(x.UserId, x.User.EmployeeCode, x.User.DisplayName, x.User.Email, x.RoleCode))
            .ToListAsync(cancellationToken));
    }

    
    [HttpGet("products")]
public async Task<IActionResult> GetProducts(
    [FromQuery] Guid categoryId,
    [FromServices] AppDbContext db,
    CancellationToken cancellationToken)
{
    var items = await db.Products                          // ← Products table
        .Where(x => x.IsActive && x.CategoryId == categoryId)  // ← CategoryId
        .OrderBy(x => x.Name)
        .Select(x => new ReferenceItemDto(x.Id, null, x.Name))
        .ToListAsync(cancellationToken);

    return Ok(items);
}

[HttpGet("workflow-defaults")]
public async Task<IActionResult> GetWorkflowDefaults(
    [FromQuery] Guid categoryId,
    [FromQuery] Guid? productId,
    [FromServices] AppDbContext db,
    CancellationToken cancellationToken)
{
    var query = db.WorkflowDefinitions
        .Where(x => x.CategoryId == categoryId)
        .Where(x => x.Status == WorkflowDefinitionStatus.Published);

    // Product-specific workflow dhundo (agar product diya ho)
    if (productId.HasValue)
    {
        query = query.Where(x => x.ProductId == productId.Value);
    }

    var workflow = await query
        .OrderByDescending(x => x.Version)
        .FirstOrDefaultAsync(cancellationToken);

    if (workflow is null)
        return Ok(new { defaultPriority = 1 });

    return Ok(new { defaultPriority = workflow.DefaultPriority });
}}
