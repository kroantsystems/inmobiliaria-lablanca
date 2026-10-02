using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Infrastructure.Authentication;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LaBlanca.Infrastructure;

public static class DependencyInjection
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' não configurada.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("LaBlanca.Migrations");
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.Schema);
        }));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.PostConfigure<StorageOptions>(o => o.RootPath = Path.GetFullPath(o.RootPath, environment.ContentRootPath));

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: [ReadyTag])
            .AddCheck<StorageHealthCheck>("storage", tags: [ReadyTag]);

        return services;
    }
}
