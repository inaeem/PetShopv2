using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PetShop.Data.Context;
using PetShop.Data.Diagnostics;

namespace PetShop.Data.Repositories;

/// <summary>Default EF Core implementation of <see cref="IRepository{T}"/>.</summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly PetShopDbContext Db;
    protected readonly DbSet<T> Set;
    protected readonly ILayerTracer Tracer;

    // Category shown as the trace line's SourceContext, e.g. "PetShop.Data.Repository<Pet>".
    private readonly string _category = $"PetShop.Data.Repository<{typeof(T).Name}>";

    public Repository(PetShopDbContext db, ILayerTracer tracer)
    {
        Db = db;
        Set = db.Set<T>();
        Tracer = tracer;
    }

    /// <summary>Wraps a value-returning data call in a data-layer trace scope.</summary>
    protected Task<TResult> Measure<TResult>(string member, Func<Task<TResult>> body, object? args = null)
        => Tracer.MeasureAsync(LayerKind.Data, _category, member, body, args);

    /// <summary>Wraps a void data call in a data-layer trace scope.</summary>
    protected Task Measure(string member, Func<Task> body, object? args = null)
        => Tracer.MeasureAsync(LayerKind.Data, _category, member, body, args);

    public virtual Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => Measure(nameof(GetByIdAsync),
            () => Set.FindAsync(new object?[] { id }, ct).AsTask(), new { id });

    public virtual Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
        => Measure<IReadOnlyList<T>>(nameof(ListAsync),
            async () => await Set.AsNoTracking().ToListAsync(ct));

    public virtual Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => Measure<IReadOnlyList<T>>(nameof(FindAsync),
            async () => await Set.AsNoTracking().Where(predicate).ToListAsync(ct));

    public IQueryable<T> Query() => Set.AsQueryable();

    public virtual Task AddAsync(T entity, CancellationToken ct = default)
        => Measure(nameof(AddAsync), async () => await Set.AddAsync(entity, ct));

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);
}
