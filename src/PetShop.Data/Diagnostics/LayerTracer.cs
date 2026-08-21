using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PetShop.Data.Diagnostics;

/// <summary>
/// Default <see cref="ILayerTracer"/>. Creates a logger whose category is the
/// caller's class name (so the trace line's SourceContext identifies the layer)
/// and writes "Enter"/"Exit" Debug lines with the elapsed time. A call that throws
/// is reported as "FAULTED" with the exception type. The correlation id and the
/// request's business identifiers flow automatically via the ambient log context, so
/// every trace line for a request shares them.
/// <para>
/// Each traced call also opens a logging scope carrying <c>Operation</c> (the
/// class.method being executed) and <c>EventType</c>; the log sink maps those to their
/// own columns. Scopes nest, and the innermost wins — so a line written while
/// PetRepository.GetByIdAsync runs reports that method, not the outer service call.
/// Arguments and return values are serialized into the message when IncludeArguments is
/// on, which is how "function inputs and outputs" reach the log table.
/// </para>
/// </summary>
public sealed class LayerTracer : ILayerTracer
{
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 8
    };

    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptionsMonitor<LayerTracingOptions> _options;

    public LayerTracer(ILoggerFactory loggerFactory, IOptionsMonitor<LayerTracingOptions> options)
    {
        _loggerFactory = loggerFactory;
        _options = options;
    }

    public async Task<T> MeasureAsync<T>(LayerKind layer, string category, string member,
        Func<Task<T>> body, object? args = null)
    {
        var logger = ResolveLogger(layer, category);
        if (logger is null) return await body();

        using var operation = OperationScope(logger, category, member);

        LogEnter(logger, member, args);
        var start = Stopwatch.GetTimestamp();
        try
        {
            var result = await body();
            LogExit(logger, member, start, fault: null, result: result);
            return result;
        }
        catch (Exception ex)
        {
            LogExit(logger, member, start, fault: ex, result: null);
            throw;
        }
    }

    public async Task MeasureAsync(LayerKind layer, string category, string member,
        Func<Task> body, object? args = null)
    {
        var logger = ResolveLogger(layer, category);
        if (logger is null) { await body(); return; }

        using var operation = OperationScope(logger, category, member);

        LogEnter(logger, member, args);
        var start = Stopwatch.GetTimestamp();
        try
        {
            await body();
            LogExit(logger, member, start, fault: null, result: null);
        }
        catch (Exception ex)
        {
            LogExit(logger, member, start, fault: ex, result: null);
            throw;
        }
    }

    /// <summary>Returns a logger if tracing is on for this layer and Debug is enabled; otherwise null.</summary>
    private ILogger? ResolveLogger(LayerKind layer, string category)
    {
        if (!_options.CurrentValue.IsEnabledFor(layer)) return null;
        var logger = _loggerFactory.CreateLogger(category);
        return logger.IsEnabled(LogLevel.Debug) ? logger : null;
    }

    /// <summary>Names the function being executed for every log line nested under this call.</summary>
    private static IDisposable? OperationScope(ILogger logger, string category, string member) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = $"{ClassName(category)}.{member}"
        });

    private void LogEnter(ILogger logger, string member, object? args)
    {
        using var scope = EventTypeScope(logger, "Enter");

        if (_options.CurrentValue.IncludeArguments && args is not null)
            logger.LogDebug("→ Enter {Member} {Args:l}", member, Serialize(args));
        else
            logger.LogDebug("→ Enter {Member}", member);
    }

    private void LogExit(ILogger logger, string member, long startTimestamp, Exception? fault, object? result)
    {
        using var scope = EventTypeScope(logger, "Exit");

        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        if (fault is not null)
        {
            logger.LogDebug("← Exit  {Member} FAULTED: {FaultType} ({Elapsed:0.0} ms)",
                member, fault.GetType().Name, elapsedMs);
        }
        else if (_options.CurrentValue.IncludeArguments && result is not null)
        {
            logger.LogDebug("← Exit  {Member} ({Elapsed:0.0} ms) {Result:l}",
                member, elapsedMs, Serialize(result));
        }
        else
        {
            logger.LogDebug("← Exit  {Member} ({Elapsed:0.0} ms)", member, elapsedMs);
        }
    }

    private static IDisposable? EventTypeScope(ILogger logger, string eventType) =>
        logger.BeginScope(new Dictionary<string, object> { ["EventType"] = eventType });

    /// <summary>"PetShop.Data.Repositories.PetRepository" → "PetRepository".</summary>
    private static string ClassName(string category)
    {
        var lastDot = category.LastIndexOf('.');
        return lastDot >= 0 && lastDot < category.Length - 1 ? category[(lastDot + 1)..] : category;
    }

    /// <summary>
    /// Best-effort JSON for a log line. Never throws: an entity with a quirky graph is a
    /// reason to log less detail, not to fail the call being traced.
    /// </summary>
    private static string Serialize(object value)
    {
        try
        {
            return JsonSerializer.Serialize(value, PayloadOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return "<unserializable>";
        }
    }
}
