using BankingPlatform.Domain.Common;

namespace BankingPlatform.Domain.Entities;

public sealed class Product : EntityBase
{
    public Guid CategoryId { get; set; }
    public ComplaintCategory Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

}