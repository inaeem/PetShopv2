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
    }
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
