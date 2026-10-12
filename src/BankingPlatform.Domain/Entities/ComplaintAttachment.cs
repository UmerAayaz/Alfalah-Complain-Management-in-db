namespace BankingPlatform.Domain.Entities;

public class ComplaintAttachment
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public Guid? WorkflowTaskId { get; set; }
    public Guid? FieldId { get; set; }

    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }

    // DB mein bytes (Pattern B)
    public byte[] Content { get; set; } = default!;

    public string? UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}