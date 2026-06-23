using PetShop.Domain.Enums;

namespace PetShop.Domain.Entities;

/// <summary>
/// A usage grouping. Each entry references exactly one subject via a polymorphic link:
/// <see cref="Type"/> says whether the subject is a Pet or a Plant, and
/// <see cref="SubjectId"/> is its primary key in the corresponding table. Because one
/// column can't carry a foreign key to two tables, there is no DB-level FK on SubjectId —
/// keeping it pointed at a live row is the application's responsibility. Maps to dbo.UsesGroups.
/// </summary>
public class UsesGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Whether <see cref="SubjectId"/> refers to a Pet or a Plant.</summary>
    public UsesGroupSubjectType Type { get; set; }

    /// <summary>Primary key of the referenced Pet or Plant, per <see cref="Type"/>.</summary>
    public int SubjectId { get; set; }

    public DateTime CreatedUtc { get; set; }
}
