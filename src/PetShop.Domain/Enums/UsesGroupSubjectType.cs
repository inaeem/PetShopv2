namespace PetShop.Domain.Enums;

/// <summary>
/// The kind of subject a <see cref="Entities.UsesGroup"/> row points at. Combined with
/// UsesGroup.SubjectId, this identifies the referenced Pet or Plant row. There is no
/// database foreign key (one column can target two tables), so referential integrity
/// for SubjectId is the application's responsibility.
/// </summary>
public enum UsesGroupSubjectType
{
    Pet = 1,
    Plant = 2
}
