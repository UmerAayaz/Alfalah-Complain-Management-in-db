using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/workflow-tasks")]
public sealed class WorkflowTasksController(IWorkflowTaskService service) : ControllerBase
{
    // ============================================================
    // MY BUCKET
    // ============================================================

    [HttpGet("my-bucket")]
    public async Task<ActionResult<IReadOnlyList<WorkflowTaskDto>>> MyBucket(CancellationToken cancellationToken)
        => Ok(await service.GetMyBucketAsync(cancellationToken));

    // ============================================================
    // ACTIONS
    // ============================================================

    [HttpGet("{taskId:guid}/actions")]
    public async Task<ActionResult<IReadOnlyList<WorkflowTaskActionDto>>> Actions(Guid taskId, CancellationToken cancellationToken)
        => Ok(await service.GetActionsAsync(taskId, cancellationToken));

    // ============================================================
    // COMPLETE TASK
    // ============================================================

    [HttpPost("{taskId:guid}/complete")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Complete(
        Guid taskId,
        [FromForm] string? outcomeKey,
        [FromForm] string? comment,
        [FromForm] Guid? nextAssigneeUserId,
        [FromForm] string? fieldAnswers,
        CancellationToken cancellationToken)
    {
        List<WorkflowFieldAnswerRequest> answers;

        if (string.IsNullOrWhiteSpace(fieldAnswers))
        {
            answers = new List<WorkflowFieldAnswerRequest>();
        }
        else
        {
            try
            {
                answers = JsonSerializer.Deserialize<List<WorkflowFieldAnswerRequest>>(
                    fieldAnswers,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<WorkflowFieldAnswerRequest>();
            }
            catch (JsonException ex)
            {
                return BadRequest(new { error = "Invalid fieldAnswers JSON.", detail = ex.Message });
            }
        }

        var request = new CompleteWorkflowTaskRequest(
            outcomeKey,
            nextAssigneeUserId,
            comment,
            answers);

        var files = Request.Form.Files;

        try
        {
            await service.CompleteAsync(taskId, request, files, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Business validation (e.g. required attachment missing)
            return BadRequest(new { error = ex.Message });
        }

        return NoContent();
    }

    // ============================================================
    // FORM (current + previous step fields)
    // ============================================================

    [HttpGet("{taskId:guid}/form")]
    public async Task<ActionResult<WorkflowTaskFormDto>> GetForm(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return Ok(
            await service.GetFormAsync(
                taskId,
                cancellationToken));
    }

    // ============================================================
    // ATTACHMENT — VIEW (inline, read from database)
    // ============================================================

    [HttpGet("attachments/{attachmentId:guid}/view")]
    public async Task<IActionResult> ViewAttachment(
        Guid attachmentId,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var att = await db.WorkflowFieldAttachments
            .AsNoTracking()
            .Where(x => x.Id == attachmentId)
            .Select(x => new { x.Content, x.ContentType })
            .FirstOrDefaultAsync(cancellationToken);

        if (att is null || att.Content is null || att.Content.Length == 0)
            return NotFound();

        return File(
            att.Content,
            att.ContentType ?? "application/octet-stream",
            enableRangeProcessing: true);
    }

    // ============================================================
    // ATTACHMENT — DOWNLOAD (read from database)
    // ============================================================

    [HttpGet("attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(
        Guid attachmentId,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var att = await db.WorkflowFieldAttachments
            .AsNoTracking()
            .Where(x => x.Id == attachmentId)
            .Select(x => new { x.Content, x.ContentType, x.FileName })
            .FirstOrDefaultAsync(cancellationToken);

        if (att is null || att.Content is null || att.Content.Length == 0)
            return NotFound();

        return File(
            att.Content,
            att.ContentType ?? "application/octet-stream",
            att.FileName);
    }
}