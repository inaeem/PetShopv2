namespace PetShop.Service.DTOs;

/// <summary>
/// One row of the configured-endpoints table, projected from a
/// <see cref="External.FhirEndpointSettings"/> entry of <c>PetSync:Endpoints</c>.
/// </summary>
public record FhirEndpointRowDto(
    string Name,
    string MetaDataUrl,
    string SwaggerUrl,
    string Url);
