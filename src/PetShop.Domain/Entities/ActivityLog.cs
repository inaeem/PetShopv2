namespace PetShop.Domain.Entities;

/// <summary>
/// An audit/activity log entry. Each row may reference a Pet and/or a Category by id. Both
/// links are nullable foreign keys created WITH NOCHECK (untrusted): existing rows are not
/// validated — PetId predates this code and CategoryId is newly added (so every existing row
/// has CategoryId = NULL) — while new writes are enforced. Maps to dbo.ActivityLogs.
/// </summary>
public class ActivityLog
{
    public long Id { get; set; }

    /// <summary>What happened, e.g. "Created", "PriceChanged", "Adopted".</summary>
    public string Action { get; set; } = string.Empty;

    // Nullable foreign keys (untrusted in the DB). A non-null value is only guaranteed to exist
    // in the parent table for rows written after the FK was added.
    public int? PetId { get; set; }
    public int? CategoryId { get; set; }

    public DateTime CreatedUtc { get; set; }
}
