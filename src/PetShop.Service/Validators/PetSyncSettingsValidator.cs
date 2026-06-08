using FluentValidation;
using PetShop.Service.External;

namespace PetShop.Service.Validators;

/// <summary>
/// Validates the <c>PetSync</c> configuration section. Each configured client
/// certificate must carry a well-formed thumbprint so a typo fails fast at
/// startup rather than as an opaque "certificate not found" on the first request.
/// </summary>
public class PetSyncSettingsValidator : AbstractValidator<PetSyncSettings>
{
    public PetSyncSettingsValidator()
    {
        RuleForEach(x => x.ClientCertificates).SetValidator(new ClientCertificateSettingsValidator());

        // FHIR endpoints: each must be well-formed and names must be unique, so a typo or
        // duplicate fails fast at startup rather than as an opaque duplicate-key throw when
        // FhirEndpointResolver builds its by-name index.
        RuleForEach(x => x.Endpoints).SetValidator(new FhirEndpointSettingsValidator());
        RuleFor(x => x.Endpoints)
            .Must(HaveUniqueNames)
            .WithMessage("PetSync FHIR endpoint names must be unique (case-insensitive).");
    }

    private static bool HaveUniqueNames(List<FhirEndpointSettings> endpoints)
    {
        var named = endpoints.Where(e => !string.IsNullOrWhiteSpace(e.Name)).ToList();
        return named.Select(e => e.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == named.Count;
    }
}

/// <summary>Validates a single configured FHIR endpoint.</summary>
public class FhirEndpointSettingsValidator : AbstractValidator<FhirEndpointSettings>
{
    public FhirEndpointSettingsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("PetSync FHIR endpoint Name is required.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("PetSync FHIR endpoint Url is required.")
            .Must(BeAnAbsoluteUrl).WithMessage("PetSync FHIR endpoint Url must be an absolute URL.");

        // Metadata/Swagger URLs are optional, but if supplied must be well-formed.
        RuleFor(x => x.MetaDataUrl)
            .Must(BeAnAbsoluteUrl).When(x => !string.IsNullOrWhiteSpace(x.MetaDataUrl))
            .WithMessage("PetSync FHIR endpoint MetaDataUrl must be an absolute URL.");

        RuleFor(x => x.SwaggerUrl)
            .Must(BeAnAbsoluteUrl).When(x => !string.IsNullOrWhiteSpace(x.SwaggerUrl))
            .WithMessage("PetSync FHIR endpoint SwaggerUrl must be an absolute URL.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("PetSync FHIR endpoint Type must be Provider or Patient.");
    }

    private static bool BeAnAbsoluteUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out _);
}

/// <summary>Validates a single mutual-TLS client certificate reference.</summary>
public class ClientCertificateSettingsValidator : AbstractValidator<ClientCertificateSettings>
{
    public ClientCertificateSettingsValidator()
    {
        RuleFor(x => x.Thumbprint)
            .NotEmpty().WithMessage("PetSync client certificate Thumbprint is required.")
            .Must(BeAValidThumbprint)
            .WithMessage("PetSync client certificate Thumbprint must be a 40-character hex (SHA-1) value; " +
                         "spaces and separators are allowed and ignored.");
    }

    /// <summary>
    /// Mirrors the normalization in <c>DependencyInjection.ResolveClientCertificate</c>:
    /// strip whitespace/separators, then require exactly 40 hexadecimal characters
    /// (the length of a SHA-1 thumbprint, which is what <c>FindByThumbprint</c> matches).
    /// </summary>
    private static bool BeAValidThumbprint(string? thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
            return false;

        var normalized = thumbprint.Where(char.IsLetterOrDigit).ToArray();
        return normalized.Length == 40 && normalized.All(Uri.IsHexDigit);
    }
}
