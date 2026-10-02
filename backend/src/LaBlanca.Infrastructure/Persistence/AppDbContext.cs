using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Domain.Analytics;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Identity;
using LaBlanca.Domain.Leads;
using LaBlanca.Domain.Media;
using LaBlanca.Domain.Owners;
using LaBlanca.Domain.Properties;
using LaBlanca.Domain.Settings;
using LaBlanca.Domain.Visits;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : DbContext(options), IAppDbContext
{
    // Fora do schema `public`, que a Data API do Supabase expõe com a chave anônima.
    public const string Schema = "lablanca";

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Zone> Zones => Set<Zone>();

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<PropertyTranslation> PropertyTranslations => Set<PropertyTranslation>();

    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    public DbSet<Lead> Leads => Set<Lead>();

    public DbSet<Owner> Owners => Set<Owner>();

    public DbSet<Visit> Visits => Set<Visit>();

    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    // Lido a cada consulta, então o filtro sempre usa o tenant da requisição atual.
    private Guid CurrentTenantId => tenantContext.TenantId;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        foreach (var enumType in typeof(Entity).Assembly.GetTypes().Where(t => t.IsEnum))
        {
            configurationBuilder.Properties(enumType).HaveConversion<string>().HaveMaxLength(32);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(t => typeof(TenantEntity).IsAssignableFrom(t.ClrType)))
        {
            GetType().GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : TenantEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
}
