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

        // Register all FluentValidation validators in this assembly.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
