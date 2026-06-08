namespace PetShop.Service.External;

/// <summary>
/// Resolves configured FHIR endpoints (from the <c>FhirEndpoints</c> section) by name
/// or type. Backed by an in-memory index built once at startup, so lookups are O(1)
/// and never re-scan the configured list.
/// </summary>
public interface IFhirEndpointResolver
{
    /// <summary>
    /// Returns the endpoint with the given name (case-insensitive), or throws
    /// <see cref="Common.AppException.NotFound"/> (404) when none matches.
    /// </summary>
    FhirEndpointSettings GetByName(string name);

    /// <summary>Returns the endpoint with the given name, or <c>false</c> if none matches.</summary>
    bool TryGetByName(string name, out FhirEndpointSettings endpoint);

    /// <summary>Returns every endpoint of the given type (empty if none).</summary>
    IReadOnlyList<FhirEndpointSettings> GetByType(EndpointType type);
}
