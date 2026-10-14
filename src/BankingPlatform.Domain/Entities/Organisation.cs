using BankingPlatform.Domain.Common;

namespace BankingPlatform.Domain.Entities;

public sealed class Department : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ComplaintCategory : EntityBase
{
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
}

public sealed class AppUser : EntityBase
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class DepartmentMembership : EntityBase
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string RoleCode { get; set; } = string.Empty;
}
