using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.Common;
using BankingPlatform.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/library-fields")]
public sealed class LibraryFieldsController(ILibraryFieldService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LibraryFieldDto>>> List(
        CancellationToken ct)
        => Ok(await service.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LibraryFieldDto>> Get(
        Guid id,
        CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<LibraryFieldDto>> Create(
        [FromBody] CreateLibraryFieldRequest request,
        CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LibraryFieldDto>> Update(
        Guid id,
        [FromBody] UpdateLibraryFieldRequest request,
        CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("regex-presets")]
    public ActionResult<IReadOnlyList<RegexPresetDto>> GetRegexPresets()
    {
        var list = RegexPresets.All
            .Select(p => new RegexPresetDto(p.Key, p.Label, p.Pattern, p.Message))
            .ToList();

        return Ok(list);
    }
}