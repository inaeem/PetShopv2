using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PetShop.Data.Diagnostics;

/// <summary>
/// Default <see cref="ILayerTracer"/>. Creates a logger whose category is the
/// caller's class name (so the trace line's SourceContext identifies the layer)
/// and writes "Enter"/"Exit" lines with the elapsed time, at the level configured by
/// <see cref="LayerTracingOptions.Level"/> — Information by default, so the internal call
/// path is captured in QA and Production and not just in verbose local runs. A call that
/// throws is reported as "FAULTED" with the exception type, always at Warning. The
/// correlation id and the request's business identifiers flow automatically via the
/// ambient log context, so every trace line for a request shares them.
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

    /// <summary>
    /// Returns a logger if tracing is on for this layer and the configured trace level is
    /// enabled for it; otherwise null. The level is Information by default so the internal
    /// call path is recorded in QA and Production too, not only in verbose local runs.
    /// </summary>
    private ILogger? ResolveLogger(LayerKind layer, string category)
    {
        var options = _options.CurrentValue;
        if (!options.IsEnabledFor(layer)) return null;

        var logger = _loggerFactory.CreateLogger(category);
        return logger.IsEnabled(options.Level) ? logger : null;
    }

    /// <summary>Names the function being executed for every log line nested under this call.</summary>
    private static IDisposable? OperationScope(ILogger logger, string category, string member) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = $"{ClassName(category)}.{member}"
        });

    private void LogEnter(ILogger logger, string member, object? args)
    {
        var options = _options.CurrentValue;
        using var scope = EventTypeScope(logger, "Enter");

        // The member name always goes out; arguments only when explicitly opted in, since
        // that is where PII lives (owner emails, request DTOs).
        if (options.IncludeArguments && args is not null)
            logger.Log(options.Level, "→ Enter {Member} {Args:l}", member, Serialize(args));
        else
            logger.Log(options.Level, "→ Enter {Member}", member);
    }

    private void LogExit(ILogger logger, string member, long startTimestamp, Exception? fault, object? result)
    {
        var options = _options.CurrentValue;
        using var scope = EventTypeScope(logger, "Exit");

        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        if (fault is not null)
        {
            // Warning regardless of the configured level: which method faulted is worth
            // seeing even when routine tracing is turned down.
            logger.LogWarning("← Exit  {Member} FAULTED: {FaultType} ({Elapsed:0.0} ms)",
                member, fault.GetType().Name, elapsedMs);
        }
        else if (options.IncludeArguments && result is not null)
        {
            logger.Log(options.Level, "← Exit  {Member} ({Elapsed:0.0} ms) {Result:l}",
                member, elapsedMs, Serialize(result));
        }
        else
        {
            logger.Log(options.Level, "← Exit  {Member} ({Elapsed:0.0} ms)", member, elapsedMs);
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
