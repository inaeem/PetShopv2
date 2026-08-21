using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetShop.Data.Context;
using PetShop.Data.Diagnostics;
using PetShop.Data.Repositories;
using PetShop.Domain.Entities;

namespace PetShop.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    // Trace SourceContext for the commit itself.
    private const string TraceCategory = "PetShop.Data.UnitOfWork";

    private readonly PetShopDbContext _db;
    private readonly ILayerTracer _tracer;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly IOptionsMonitor<LayerTracingOptions> _options;

    // Repositories are injected (not new-ed) so cross-cutting dependencies such as
    // the layer tracer flow in via DI. All share the same scoped DbContext.
    public UnitOfWork(
        PetShopDbContext db,
        IPetRepository pets,
        IRepository<Category> categories,
        ILayerTracer tracer,
        ILogger<UnitOfWork> logger,
        IOptionsMonitor<LayerTracingOptions> options)
    {
        _db = db;
        Pets = pets;
        Categories = categories;
        _tracer = tracer;
        _logger = logger;
        _options = options;
    }

    public IPetRepository Pets { get; }
    public IRepository<Category> Categories { get; }

    /// <summary>
    /// Commits the unit of work, traced like any other data call, and — just before the
    /// write — logs one row per entity the change tracker is about to persist.
    /// <para>
    /// Those rows are the domain layer's footprint in the log. PetShop.Domain is
    /// deliberately dependency-free (plain entities, no logging package), so it cannot log
    /// for itself; the change tracker knows exactly which entity changed, in which
    /// direction, and which properties moved, which is the same information a domain event
    /// would carry — without putting a logger inside the domain.
    /// </para>
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _tracer.MeasureAsync(LayerKind.Data, TraceCategory, nameof(SaveChangesAsync), async () =>
        {
            LogPendingChanges();
            return await _db.SaveChangesAsync(ct);
        });

    private void LogPendingChanges()
    {
        var options = _options.CurrentValue;
        if (!options.IsEnabledFor(LayerKind.Data)) return;
        if (!_logger.IsEnabled(options.Level)) return;

        foreach (var entry in _db.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["EventType"] = "Domain"
            });

            _logger.Log(options.Level, "{ChangeKind} {Entity} {EntityKey} {Changes:l}",
                entry.State, entry.Metadata.ClrType.Name, KeyOf(entry),
                DescribeChanges(entry, options.IncludeArguments));
        }
    }

    /// <summary>Primary key value, or "?" for an insert whose identity hasn't been generated yet.</summary>
    private static string KeyOf(EntityEntry entry)
    {
        var key = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue;
        return key?.ToString() is { Length: > 0 } value && value != "0" ? value : "?";
    }

    /// <summary>
    /// Which properties changed. Values are included only when IncludeArguments is on —
    /// entity fields carry PII (owner emails), so the default is property names alone.
    /// </summary>
    private static string DescribeChanges(EntityEntry entry, bool includeValues)
    {
        if (entry.State != EntityState.Modified) return "";

        var changed = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => includeValues
                ? $"{p.Metadata.Name}: {p.OriginalValue} → {p.CurrentValue}"
                : p.Metadata.Name);

        return string.Join(", ", changed);
    }
}
