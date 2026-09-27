using BankingPlatform.Domain.Common;

namespace BankingPlatform.Domain.Entities;

public class WorkflowFieldAttachment : EntityBase
{
    public Guid WorkflowFieldResponseId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string? Sha256Hash { get; set; }

    public Guid UploadedByUserId { get; set; }
}