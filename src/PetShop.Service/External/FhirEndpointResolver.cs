using Microsoft.Extensions.Options;
using PetShop.Service.Common;

namespace PetShop.Service.External;

/// <summary>
/// Default <see cref="IFhirEndpointResolver"/>. Registered as a singleton: the
/// <see cref="PetSyncSettings"/> snapshot is read once via <see cref="IOptions{T}"/> and
/// its <see cref="PetSyncSettings.Endpoints"/> are projected into name/type indexes at
/// construction. Duplicate names are rejected at startup by the validator, so the name
/// dictionary build is safe.
/// </summary>
public class FhirEndpointResolver : IFhirEndpointResolver
{
    private readonly IReadOnlyDictionary<string, FhirEndpointSettings> _byName;
    private readonly ILookup<EndpointType, FhirEndpointSettings> _byType;

    public FhirEndpointResolver(IOptions<PetSyncSettings> options)
    {
        var endpoints = options.Value.Endpoints;
        _byName = endpoints.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
        _byType = endpoints.ToLookup(e => e.Type);
    }

    public FhirEndpointSettings GetByName(string name) =>
        _byName.TryGetValue(name, out var endpoint)
            ? endpoint
            : throw AppException.NotFound($"FHIR endpoint '{name}'");

    public bool TryGetByName(string name, out FhirEndpointSettings endpoint) =>
        _byName.TryGetValue(name, out endpoint!);

    public IReadOnlyList<FhirEndpointSettings> GetByType(EndpointType type) =>
        _byType[type].ToList();
}
