using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetShop.Service.Security;
using PetShop.Service.Services;

namespace PetShop.Service;

public static class DependencyInjection
{
    /// <summary>Registers services, validators and security helpers.</summary>
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<IPetService, PetService>();
        services.AddScoped<IAuthService, AuthService>();

        // Register all FluentValidation validators in this assembly.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
