using FluentValidation;
using PetShop.Service.External;

namespace PetShop.Service.Validators;

/// <summary>
/// Validates the <c>FhirEndpoints</c> configuration section. Each endpoint must carry a
/// name and a well-formed absolute URL, and names must be unique — so a typo or duplicate
/// fails fast at startup rather than as an opaque error (e.g. a duplicate-key throw when
/// <see cref="FhirEndpointResolver"/> builds its by-name index).
/// </summary>
public class FhirEndpointsSettingsValidator : AbstractValidator<FhirEndpointsSettings>
{
    public FhirEndpointsSettingsValidator()
    {
        RuleForEach(x => x.Endpoints).SetValidator(new FhirEndpointSettingsValidator());

        RuleFor(x => x.Endpoints)
            .Must(HaveUniqueNames)
            .WithMessage("FHIR endpoint names must be unique (case-insensitive).");
    }

    private static bool HaveUniqueNames(List<FhirEndpointSettings> endpoints) =>
        endpoints
            .Select(e => e.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == endpoints.Count(e => !string.IsNullOrWhiteSpace(e.Name));
}

/// <summary>Validates a single configured FHIR endpoint.</summary>
public class FhirEndpointSettingsValidator : AbstractValidator<FhirEndpointSettings>
{
    public FhirEndpointSettingsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("FHIR endpoint Name is required.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("FHIR endpoint Url is required.")
            .Must(BeAnAbsoluteUrl).WithMessage("FHIR endpoint Url must be an absolute URL.");

        // Metadata/Swagger URLs are optional, but if supplied must be well-formed.
        RuleFor(x => x.MetaDataUrl)
            .Must(BeAnAbsoluteUrl).When(x => !string.IsNullOrWhiteSpace(x.MetaDataUrl))
            .WithMessage("FHIR endpoint MetaDataUrl must be an absolute URL.");

        RuleFor(x => x.SwaggerUrl)
            .Must(BeAnAbsoluteUrl).When(x => !string.IsNullOrWhiteSpace(x.SwaggerUrl))
            .WithMessage("FHIR endpoint SwaggerUrl must be an absolute URL.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("FHIR endpoint Type must be Provider or Patient.");
    }

    private static bool BeAnAbsoluteUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out _);
}
