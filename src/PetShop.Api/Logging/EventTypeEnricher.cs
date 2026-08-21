using Serilog.Core;
using Serilog.Events;

namespace PetShop.Api.Logging;

/// <summary>
/// Guarantees every log row has an <c>EventType</c>, so the column is never NULL and
/// "show me all X" queries return complete results.
/// <para>
/// Writers that know what kind of row they are producing push the value themselves —
/// Request/Response, Enter/Exit, ExternalRequest/ExternalResponse, Sql, Domain,
/// Validation, DomainError, Exception, Action, Summary. This enricher only fills the gap
/// for everything else: an ordinary application log line becomes <c>Business</c>, and
/// anything carrying an exception (or logged at Error and above) becomes
/// <c>Exception</c>.
/// </para>
/// <para>
/// Must be registered AFTER <c>Enrich.FromLogContext()</c>: the pushed value has to be on
/// the event already, otherwise every row would be labelled Business.
/// </para>
/// </summary>
public sealed class EventTypeEnricher : ILogEventEnricher
{
    public const string PropertyName = "EventType";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.ContainsKey(PropertyName)) return;

        var eventType = logEvent.Exception is not null || logEvent.Level >= LogEventLevel.Error
            ? "Exception"
            : "Business";

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, eventType));
    }
}
