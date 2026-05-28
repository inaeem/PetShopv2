namespace PetShop.Data.Diagnostics;

/// <summary>
/// Wraps a method call in configurable entry/exit Debug logs. Because the body is
/// passed as a delegate, the exit line can report whether the call completed or
/// FAULTED. Usage:
/// <code>
/// public Task&lt;PetDto&gt; GetByIdAsync(int id) =&gt;
///     _tracer.MeasureAsync(LayerKind.Service, Category, nameof(GetByIdAsync),
///         async () =&gt; (await _uow.Pets.GetWithCategoryAsync(id))!.ToDto(), new { id });
/// </code>
/// When tracing is disabled the body is simply awaited with no logging overhead.
/// </summary>
public interface ILayerTracer
{
    /// <summary>Traces a value-returning async call.</summary>
    Task<T> MeasureAsync<T>(LayerKind layer, string category, string member,
        Func<Task<T>> body, object? args = null);

    /// <summary>Traces a non-value (void) async call.</summary>
    Task MeasureAsync(LayerKind layer, string category, string member,
        Func<Task> body, object? args = null);
}
