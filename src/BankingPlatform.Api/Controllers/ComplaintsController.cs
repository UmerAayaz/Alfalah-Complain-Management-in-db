using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/complaints")]
public sealed class ComplaintsController(IComplaintService service) : ControllerBase
{
    // ============================================================
    // COMPLAINTS
    // ============================================================

    [HttpPost]
    public async Task<ActionResult<ComplaintDetailsDto>> Create(CreateComplaintRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComplaintDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ComplaintListItemDto>>> List(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
        => Ok(await service.ListAsync(departmentId, categoryId, status, cancellationToken));

    // ============================================================
    // ATTACHMENTS
    // ============================================================

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(50_000_000)] // 50 MB per request
    public async Task<IActionResult> UploadAttachments(
        Guid id,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
            return BadRequest(new { message = "No files uploaded." });

        var inputs = new List<UploadAttachmentInput>();

        foreach (var f in files)
        {
            if (f.Length == 0) continue;

            inputs.Add(new UploadAttachmentInput(
                f.OpenReadStream(),
                f.FileName,
                f.ContentType ?? "application/octet-stream",
                f.Length));
        }

        var result = await service.UploadAttachmentsAsync(id, inputs, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAttachmentsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(
        Guid attachmentId,
        [FromQuery] bool inline = false,
        CancellationToken cancellationToken = default)
    {
        var result = await service.DownloadAttachmentAsync(attachmentId, cancellationToken);

        if (result is null)
            return NotFound();

        var disposition = inline ? "inline" : "attachment";
        Response.Headers["Content-Disposition"] =
            $"{disposition}; filename=\"{result.Value.FileName}\"";

        return File(result.Value.Content, result.Value.ContentType);
    }
}