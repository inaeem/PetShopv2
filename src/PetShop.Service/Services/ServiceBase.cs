using PetShop.Data.Diagnostics;
using PetShop.Service.Security;

namespace PetShop.Service.Services;

/// <summary>
/// Base class for every service in the service layer. Taking <see cref="ICurrentUser"/>
/// as a required constructor dependency means a service cannot be constructed without
/// the caller's identity — DI must supply it. Also centralises the data/service trace
/// <see cref="Measure{T}"/> helpers that each service would otherwise duplicate.
/// </summary>
/// <remarks>
/// Inheriting this is enforced by an architecture test
/// (<c>PetShop.Tests.Architecture.ServiceLayerArchitectureTests</c>): the compiler can
/// guarantee the dependency for classes that inherit, but only the test guarantees that
/// <em>every</em> service does.
/// </remarks>
public abstract class ServiceBase
{
    /// <summary>The authenticated caller for the current request.</summary>
    protected readonly ICurrentUser CurrentUser;

    protected readonly ILayerTracer Tracer;

    // Trace SourceContext, e.g. "PetShop.Service.PetService" — derived from the concrete type.
    private readonly string _category;

    protected ServiceBase(ICurrentUser currentUser, ILayerTracer tracer)
    {
        CurrentUser = currentUser;
        Tracer = tracer;
        _category = $"PetShop.Service.{GetType().Name}";
    }

    /// <summary>Wraps a value-returning call in a service-layer trace scope.</summary>
    protected Task<T> Measure<T>(string member, object? args, Func<Task<T>> body)
        => Tracer.MeasureAsync(LayerKind.Service, _category, member, body, args);

    /// <summary>Wraps a void call in a service-layer trace scope.</summary>
    protected Task Measure(string member, object? args, Func<Task> body)
        => Tracer.MeasureAsync(LayerKind.Service, _category, member, body, args);
}
