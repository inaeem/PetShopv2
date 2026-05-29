using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetShop.Data.Context;
using PetShop.Data.Diagnostics;
using PetShop.Data.Repositories;
using PetShop.Data.UnitOfWork;
using PetShop.Domain.Entities;

namespace PetShop.Data;

public static class DependencyInjection
{
    /// <summary>Registers the DbContext, repositories and unit of work.</summary>
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PetShopDb")
            ?? throw new InvalidOperationException("Connection string 'PetShopDb' is not configured.");

        services.AddDbContext<PetShopDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(PetShopDbContext).Assembly.FullName)
                   .EnableRetryOnFailure()));

        // Configurable per-layer entry/exit tracing (IOptionsMonitor → runtime toggle).
        services.Configure<LayerTracingOptions>(configuration.GetSection(LayerTracingOptions.SectionName));
        services.AddSingleton<ILayerTracer, LayerTracer>();

        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IRepository<Category>, Repository<Category>>();
        services.AddScoped<IUnitOfWork, global::PetShop.Data.UnitOfWork.UnitOfWork>();

        return services;
    }
}
