using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/workflow-definitions")]
public sealed class WorkflowDefinitionsController(IWorkflowDefinitionService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkflowDefinitionDto>> Create(CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> Update(Guid id, CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken)
        => Ok(await service.UpdateDraftAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<WorkflowDefinitionDto>> Publish(Guid id, CancellationToken cancellationToken)
        => Ok(await service.PublishAsync(id, cancellationToken));

    [HttpPost("{id:guid}/new-version")]
    public async Task<ActionResult<WorkflowDefinitionDto>> NewVersion(Guid id, CancellationToken cancellationToken)
        => Ok(await service.CreateNewVersionAsync(id, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkflowDefinitionDto>>> List([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
        => Ok(await service.ListAsync(categoryId, cancellationToken));

    [HttpPost("migrate-form")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<MigrateFormResult>> MigrateForm(
    [FromBody] MigrateFormRequest request,
    CancellationToken ct = default)
{
    try
    {
        var result = await service.MigrateFormAsync(request, ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    catch (ArgumentException ex) { return BadRequest(ex.Message); }
}
}
