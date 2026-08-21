using CampusCore.Domain.Common;

namespace CampusCore.Domain.Entities;

public sealed class SchoolClass : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}

public sealed class Section : AuditableEntity
{
    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; } = 40;
}
