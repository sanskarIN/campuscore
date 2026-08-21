using CampusCore.Domain.Common;

namespace CampusCore.Domain.Entities;

public sealed class AcademicYear : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool IsActive { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("Academic year name is required.");
        if (EndsOn <= StartsOn) throw new ArgumentException("Academic year end date must be after its start date.");
    }
}
