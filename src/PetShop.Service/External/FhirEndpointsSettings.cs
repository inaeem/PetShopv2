namespace PetShop.Service.External;

/// <summary>
/// Bound from the "FhirEndpoints" section of appsettings.json. A configured list of
/// external FHIR endpoints this app talks to, looked up at runtime by name or type
/// (see <see cref="IFhirEndpointResolver"/>).
/// </summary>
public class FhirEndpointsSettings
{
    public const string SectionName = "FhirEndpoints";

    /// <summary>The configured endpoints. Names must be unique (see the validator).</summary>
    public List<FhirEndpointSettings> Endpoints { get; set; } = new();
}

/// <summary>A single external FHIR endpoint.</summary>
public class FhirEndpointSettings
{
    /// <summary>Unique, human-readable identifier used to look the endpoint up.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>FHIR capability/metadata statement URL (e.g. <c>.../fhir/metadata</c>).</summary>
    public string MetaDataUrl { get; set; } = string.Empty;

    /// <summary>OpenAPI/Swagger document URL.</summary>
    public string SwaggerUrl { get; set; } = string.Empty;

    /// <summary>Base service URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>What kind of endpoint this is. Bound by name ("Provider"/"Patient").</summary>
    public EndpointType Type { get; set; }
}

/// <summary>Classifies a configured FHIR endpoint.</summary>
public enum EndpointType
{
    Provider,
    Patient
}
