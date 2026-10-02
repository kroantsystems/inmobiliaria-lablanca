using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Security;
using LaBlanca.Infrastructure.Authentication;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.Infrastructure.Storage;
using LaBlanca.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.TryAddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<TenantAuditInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) => ConfigureDbContext(options, connectionString)
            .AddInterceptors(provider.GetRequiredService<TenantAuditInterceptor>()));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IAccessTokenBlacklist, MemoryAccessTokenBlacklist>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        if (configuration.GetValue("BackgroundJobs:Enabled", true))
        {
            services.AddHostedService<RefreshTokenCleanupService>();
        }

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.PostConfigure<StorageOptions>(o => o.RootPath = Path.GetFullPath(o.RootPath, environment.ContentRootPath));

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: [ReadyTag])
            .AddCheck<StorageHealthCheck>("storage", tags: [ReadyTag]);

        return services;
    }

    public static DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("LaBlanca.Migrations");
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.Schema);
            })
            .ReplaceService<IMigrationsSqlGenerator, RlsMigrationsSqlGenerator>();
}
