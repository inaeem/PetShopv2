using System.Net.Http.Headers;
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
}
