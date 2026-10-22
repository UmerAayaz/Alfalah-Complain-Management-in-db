using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/forms")]
public sealed class FormsController(IFormService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FormListItemDto>>> List(
        [FromQuery] bool includeAllVersions = false,
        CancellationToken ct = default)
        => Ok(await service.ListAsync(includeAllVersions, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FormDto>> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await service.GetAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPost]
    public async Task<ActionResult<FormDto>> Create(
        [FromBody] CreateFormRequest request,
        CancellationToken ct)
    {
        try
        {
            var created = await service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FormDto>> Update(
        Guid id,
        [FromBody] UpdateFormRequest request,
        CancellationToken ct)
    {
        try { return Ok(await service.UpdateAsync(id, request, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await service.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<FormDto>> Publish(Guid id, CancellationToken ct)
    {
        try { return Ok(await service.PublishAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("{id:guid}/new-version")]
    public async Task<ActionResult<FormDto>> CreateNewVersion(Guid id, CancellationToken ct)
    {
        try { return Ok(await service.CreateNewVersionAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}