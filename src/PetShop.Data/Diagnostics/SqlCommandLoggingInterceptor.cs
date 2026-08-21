using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PetShop.Data.Diagnostics;

/// <summary>
/// Logs every SQL command EF Core executes, with its duration, as an ordinary log row so it
/// lands in dbo.AppLogs next to the repository call that issued it — same correlation id,
/// same business identifiers, EventType "Sql".
/// <para>
/// An interceptor rather than EF's own "Microsoft.EntityFrameworkCore.Database.Command"
/// category, because that category writes free-form text at a level shared with the rest of
/// EF's diagnostics: no duration in its own column, no EventType, and no way to switch SQL
/// on without also raising EF's other noise. Here it is one flag —
/// <see cref="LayerTracingOptions.Sql"/> — and the rows match every other row in the table.
/// </para>
/// <para>
/// Off by default: a single repository call can issue several statements, so this is the
/// highest-volume source in the log. Parameter values follow IncludeArguments; the names
/// and types are always safe to show.
/// </para>
/// </summary>
public sealed class SqlCommandLoggingInterceptor : DbCommandInterceptor
{
    /// <summary>SourceContext for these rows, so they can be filtered as a group.</summary>
    private const string Category = "PetShop.Data.Sql";

    /// <summary>Long statements are trimmed — the shape of the query is what matters here.</summary>
    private const int MaxCommandTextChars = 2000;

    private readonly ILogger _logger;
    private readonly IOptionsMonitor<LayerTracingOptions> _options;

    public SqlCommandLoggingInterceptor(ILoggerFactory loggerFactory, IOptionsMonitor<LayerTracingOptions> options)
    {
        _logger = loggerFactory.CreateLogger(Category);
        _options = options;
    }

    // ---- executed ---------------------------------------------------------------------

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogExecuted(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
        CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        LogExecuted(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogExecuted(command, eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command,
        CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        LogExecuted(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogExecuted(command, eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command,
        CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        LogExecuted(command, eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    // ---- failed -----------------------------------------------------------------------

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        LogFailed(command, eventData);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        LogFailed(command, eventData);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    // ---- writing ----------------------------------------------------------------------

    private void LogExecuted(DbCommand command, CommandExecutedEventData eventData)
    {
        var options = _options.CurrentValue;
        if (!options.IsEnabledFor(LayerKind.Sql)) return;
        if (!_logger.IsEnabled(options.Level)) return;

        using var scope = EventTypeScope();
        _logger.Log(options.Level, "SQL ({Elapsed:0.0} ms) {CommandText:l}{Parameters:l}",
            eventData.Duration.TotalMilliseconds,
            Flatten(command.CommandText),
            DescribeParameters(command, options.IncludeArguments));
    }

    /// <summary>
    /// A failed statement is logged whenever SQL tracing is on, at Warning regardless of the
    /// configured level — the exception itself surfaces elsewhere, but not the statement
    /// that produced it.
    /// </summary>
    private void LogFailed(DbCommand command, CommandErrorEventData eventData)
    {
        var options = _options.CurrentValue;
        if (!options.IsEnabledFor(LayerKind.Sql)) return;

        using var scope = EventTypeScope();
        _logger.LogWarning(eventData.Exception, "SQL FAILED ({Elapsed:0.0} ms) {CommandText:l}{Parameters:l}",
            eventData.Duration.TotalMilliseconds,
            Flatten(command.CommandText),
            DescribeParameters(command, options.IncludeArguments));
    }

    private IDisposable? EventTypeScope() =>
        _logger.BeginScope(new Dictionary<string, object> { ["EventType"] = "Sql" });

    /// <summary>One-line form of the statement, trimmed to a sane length.</summary>
    private static string Flatten(string commandText)
    {
        var flattened = commandText
            .Replace("\r\n", " ")
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Replace('\t', ' ');

        while (flattened.Contains("  ", StringComparison.Ordinal))
            flattened = flattened.Replace("  ", " ", StringComparison.Ordinal);

        flattened = flattened.Trim();
        return flattened.Length <= MaxCommandTextChars
            ? flattened
            : flattened[..MaxCommandTextChars] + "…[truncated]";
    }

    /// <summary>
    /// " [@p0=42, @p1=Rex]" when values are allowed, " [@p0, @p1]" otherwise. Parameter
    /// values are the row's PII risk — an owner email or a search term is a parameter — so
    /// they are behind the same IncludeArguments switch as method arguments.
    /// </summary>
    private static string DescribeParameters(DbCommand command, bool includeValues)
    {
        if (command.Parameters.Count == 0) return "";

        var builder = new StringBuilder(" [");
        for (var i = 0; i < command.Parameters.Count; i++)
        {
            var parameter = command.Parameters[i];
            if (i > 0) builder.Append(", ");
            builder.Append(parameter.ParameterName);
            if (includeValues) builder.Append('=').Append(parameter.Value);
        }

        return builder.Append(']').ToString();
    }
}
