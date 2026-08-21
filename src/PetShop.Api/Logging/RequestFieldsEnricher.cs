using PetShop.Service.Common;
using Serilog.Core;
using Serilog.Events;

namespace PetShop.Api.Logging;

/// <summary>
/// Copies the current request's business identifiers onto every log event, so they land
/// in their own columns in dbo.AppLogs.
/// <para>
/// Runs at write time rather than at push time, which is what makes late-discovered
/// values work: a PayerId set half-way through the call appears on every row written
/// afterwards, with no <c>using</c> block around the discovery point. Rows written
/// before it was known simply have NULL there — they are still tied to the same request
/// by CorrelationRef.
/// </para>
/// <para>
/// Uses AddPropertyIfAbsent, so an explicit property on the log call (for example
/// <c>LogInformation("Reassigned to {PayerId}", other)</c>) wins over the ambient value.
/// </para>
/// </summary>
public sealed class RequestFieldsEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var fields = RequestLogContext.Current;
        if (fields is null) return;   // startup, hosted services, background work

        Add(logEvent, propertyFactory, "CorrelationId", fields.CorrelationId);
        Add(logEvent, propertyFactory, "AppRegistrationId", fields.AppRegistrationId);
        Add(logEvent, propertyFactory, "PayerId", fields.PayerId);
        Add(logEvent, propertyFactory, "ProviderId", fields.ProviderId);
        Add(logEvent, propertyFactory, "UserEmail", fields.UserEmail);
    }

    private static void Add(LogEvent logEvent, ILogEventPropertyFactory factory, string name, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        logEvent.AddPropertyIfAbsent(factory.CreateProperty(name, value));
    }
}
