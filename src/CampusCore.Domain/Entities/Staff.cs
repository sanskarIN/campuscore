using CampusCore.Domain.Common;
using CampusCore.Domain.Enums;

namespace CampusCore.Domain.Entities;

public sealed class StaffMember : AuditableEntity
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public StaffStatus Status { get; set; } = StaffStatus.Active;
}
