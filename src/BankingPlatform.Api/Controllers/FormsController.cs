using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/forms")]
public sealed class FormsController(IFormService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FormListItemDto>>> List(CancellationToken ct)
        => Ok(await service.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FormDto>> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<FormDto>> Create(
        [FromBody] CreateFormRequest request,
        CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FormDto>> Update(
        Guid id,
        [FromBody] UpdateFormRequest request,
        CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

     [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<FormDto>> Publish(Guid id, CancellationToken ct)
        => Ok(await service.PublishAsync(id, ct));

    [HttpPost("{id:guid}/new-version")]
    public async Task<ActionResult<FormDto>> CreateNewVersion(Guid id, CancellationToken ct)
        => Ok(await service.CreateNewVersionAsync(id, ct));

}