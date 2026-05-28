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

    /// <summary>Trace the data layer (repositories).</summary>
    public bool Data { get; set; } = true;

    /// <summary>
    /// Also log method arguments. Off by default — arguments can contain PII;
    /// callers are still expected not to pass secrets (e.g. passwords).
    /// </summary>
    public bool IncludeArguments { get; set; }

    public bool IsEnabledFor(LayerKind layer) => Enabled && layer switch
    {
        LayerKind.Service => Service,
        LayerKind.Data => Data,
        _ => false
    };
}
