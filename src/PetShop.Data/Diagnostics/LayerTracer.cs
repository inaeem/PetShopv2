using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PetShop.Data.Diagnostics;

/// <summary>
/// Default <see cref="ILayerTracer"/>. Creates a logger whose category is the
/// caller's class name (so the trace line's SourceContext identifies the layer)
/// and writes "Enter"/"Exit" Debug lines with the elapsed time. A call that throws
/// is reported as "FAULTED" with the exception type. The correlation id flows
/// automatically via the Serilog LogContext, so every trace line for a request
/// shares the request's id.
/// </summary>
public sealed class LayerTracer : ILayerTracer
{
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

        LogEnter(logger, member, args);
        var start = Stopwatch.GetTimestamp();
        try
        {
            var result = await body();
            LogExit(logger, member, start, fault: null);
            return result;
        }
        catch (Exception ex)
        {
            LogExit(logger, member, start, fault: ex);
            throw;
        }
    }

    public async Task MeasureAsync(LayerKind layer, string category, string member,
        Func<Task> body, object? args = null)
    {
        var logger = ResolveLogger(layer, category);
        if (logger is null) { await body(); return; }

        LogEnter(logger, member, args);
        var start = Stopwatch.GetTimestamp();
        try
        {
            await body();
            LogExit(logger, member, start, fault: null);
        }
        catch (Exception ex)
        {
            LogExit(logger, member, start, fault: ex);
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

    private void LogEnter(ILogger logger, string member, object? args)
    {
        if (_options.CurrentValue.IncludeArguments && args is not null)
            logger.LogDebug("→ Enter {Member} {@Args}", member, args);
        else
            logger.LogDebug("→ Enter {Member}", member);
    }

    private static void LogExit(ILogger logger, string member, long startTimestamp, Exception? fault)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        if (fault is null)
            logger.LogDebug("← Exit  {Member} ({Elapsed:0.0} ms)", member, elapsedMs);
        else
            logger.LogDebug("← Exit  {Member} FAULTED: {FaultType} ({Elapsed:0.0} ms)",
                member, fault.GetType().Name, elapsedMs);
    }
}
