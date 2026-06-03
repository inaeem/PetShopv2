using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetShop.Service.External;
using PetShop.Service.Services;

namespace PetShop.Service;

public static class DependencyInjection
{
    /// <summary>Registers services, validators and security helpers.</summary>
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPetService, PetService>();

        // External pet-sync client (typed HttpClient). A newly-created pet is
        // replicated to this service best-effort; see PetSyncClient. The remote
        // service does authorization only: a fixed service token from config is
        // sent as a bearer token on every request.
        services.Configure<PetSyncSettings>(configuration.GetSection(PetSyncSettings.SectionName));
        var petSync = configuration.GetSection(PetSyncSettings.SectionName).Get<PetSyncSettings>() ?? new PetSyncSettings();

        services.AddHttpClient<IPetSyncClient, PetSyncClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(petSync.BaseUrl))
                client.BaseAddress = new Uri(petSync.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(petSync.TimeoutSeconds <= 0 ? 10 : petSync.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(petSync.ServiceToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", petSync.ServiceToken);

            // Advertise what we accept back from the remote service.
            if (!string.IsNullOrWhiteSpace(petSync.Accept))
                client.DefaultRequestHeaders.Accept.ParseAdd(petSync.Accept);

            // Arbitrary extra headers sent on every request.
            foreach (var (name, value) in petSync.Headers)
                client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            // Handler-level transport options: pre-emptive auth and mutual-TLS certs.
            var handler = new HttpClientHandler { PreAuthenticate = petSync.PreAuthenticate };
            foreach (var cert in petSync.ClientCertificates)
                handler.ClientCertificates.Add(ResolveClientCertificate(cert));
            if (handler.ClientCertificates.Count > 0)
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            return handler;
        });

        // Outbound email (SMTP via System.Net.Mail). Gated by Mail:Enabled — when off,
        // sends are skipped, so non-configured environments make no SMTP calls.
        services.Configure<MailSettings>(configuration.GetSection(MailSettings.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        // Renders email bodies/subjects from on-disk templates (one folder per template).
        services.AddSingleton<IEmailTemplateRenderer, FileEmailTemplateRenderer>();

        // Register all FluentValidation validators in this assembly.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }

    /// <summary>
    /// Looks up a client certificate by thumbprint in the configured X509 store.
    /// Throws if the thumbprint is missing or no matching certificate is found, so
    /// a misconfigured mutual-TLS setup fails fast at startup rather than per-request.
    /// </summary>
    private static X509Certificate2 ResolveClientCertificate(ClientCertificateSettings cert)
    {
        if (string.IsNullOrWhiteSpace(cert.Thumbprint))
            throw new InvalidOperationException("PetSync client certificate is missing a Thumbprint.");

        // Normalize: drop whitespace/separators so values copied from certmgr ("‎ab cd …") still match.
        var thumbprint = new string(cert.Thumbprint.Where(char.IsLetterOrDigit).ToArray());

        using var store = new X509Store(cert.StoreName, cert.StoreLocation);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

        // validOnly: false so an expired-but-installed cert is still surfaced (with a clear error path).
        var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        if (found.Count == 0)
            throw new InvalidOperationException(
                $"PetSync client certificate with thumbprint '{thumbprint}' not found in " +
                $"{cert.StoreLocation}/{cert.StoreName}.");

        return found[0];
    }
}
