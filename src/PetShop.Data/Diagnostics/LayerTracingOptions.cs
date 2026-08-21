using Microsoft.Extensions.Logging;

namespace PetShop.Data.Diagnostics;

/// <summary>
/// Bound from the "Diagnostics:LayerTracing" configuration section. Bound via
/// IOptionsMonitor so it can be toggled at runtime by editing appsettings.json
/// (no restart needed). Note that trace lines are emitted at Debug level, so the
/// effective gate is: this flag AND the logger's minimum level allowing Debug.
/// </summary>
public class LayerTracingOptions
{
    public const string SectionName = "Diagnostics:LayerTracing";

    /// <summary>Master switch for entry/exit tracing.</summary>
    public bool Enabled { get; set; }

    /// <summary>Trace the service layer.</summary>
    public bool Service { get; set; } = true;

    /// <summary>Trace the data layer (repositories, unit of work).</summary>
    public bool Data { get; set; } = true;

    /// <summary>
    /// Trace outbound integrations owned by the service layer — SMTP mail, partner APIs.
    /// On by default: these are the calls you cannot reproduce after the fact, and they
    /// are low-volume compared with repository traffic.
    /// </summary>
    public bool External { get; set; } = true;

    /// <summary>
    /// Log every SQL command EF Core executes, with its duration. Off by default: it is
    /// the highest-volume source in the table (several statements per repository call) and
    /// the command text is only useful when you are actually investigating the database.
    /// Parameter values follow <see cref="IncludeArguments"/>; names are always shown.
    /// </summary>
    public bool Sql { get; set; }

    /// <summary>
    /// Level the enter/exit lines are written at. Information by default, so the internal
    /// call path (which service method, which repository method, how long) is captured in
    /// QA and Production, where the minimum level is Information. Set to "Debug" to make
    /// tracing follow a verbose-logging switch instead of <see cref="Enabled"/>.
    /// <para>
    /// A call that throws is always reported at Warning regardless of this setting — a
    /// faulted method is worth seeing even when routine tracing is turned down.
    /// </para>
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Information;

    /// <summary>
    /// Also log method arguments. Off by default — arguments can contain PII;
    /// callers are still expected not to pass secrets (e.g. passwords).
    /// </summary>
    public bool IncludeArguments { get; set; }

    public bool IsEnabledFor(LayerKind layer) => Enabled && layer switch
    {
        LayerKind.Service => Service,
        LayerKind.Data => Data,
        LayerKind.External => External,
        LayerKind.Sql => Sql,
        _ => false
    };
}
