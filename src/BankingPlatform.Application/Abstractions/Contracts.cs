using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;

namespace BankingPlatform.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IComplaintService
{
    Task<ComplaintDetailsDto> CreateAsync(CreateComplaintRequest request, CancellationToken cancellationToken);
    Task<ComplaintDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ComplaintListItemDto>> ListAsync(Guid? departmentId, Guid? categoryId, string? status, CancellationToken cancellationToken);

    // ---- Attachments ----
    Task<IReadOnlyList<ComplaintAttachmentDto>> UploadAttachmentsAsync(
        Guid complaintId,
        IReadOnlyList<UploadAttachmentInput> files,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ComplaintAttachmentDto>> GetAttachmentsAsync(
        Guid complaintId,
        CancellationToken cancellationToken);

    Task<(byte[] Content, string FileName, string ContentType)?> DownloadAttachmentAsync(
        Guid attachmentId,
        CancellationToken cancellationToken);
}

public interface IWorkflowDefinitionService
{
    Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken);
    Task<WorkflowDefinitionDto> UpdateDraftAsync(Guid id, CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken);
    Task<WorkflowDefinitionDto> PublishAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowDefinitionDto> CreateNewVersionAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowDefinitionDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowDefinitionDto>> ListAsync(Guid? categoryId, CancellationToken cancellationToken);
}