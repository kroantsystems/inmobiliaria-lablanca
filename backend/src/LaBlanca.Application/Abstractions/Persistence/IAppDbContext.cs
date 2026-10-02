using LaBlanca.Domain.Analytics;
using LaBlanca.Domain.Identity;
using LaBlanca.Domain.Leads;
using LaBlanca.Domain.Media;
using LaBlanca.Domain.Owners;
using LaBlanca.Domain.Properties;
using LaBlanca.Domain.Settings;
using LaBlanca.Domain.Visits;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Abstractions.Persistence;

/// <summary>Consultas já filtradas pelo tenant atual; use <c>IgnoreQueryFilters</c> só em rotinas sem tenant.</summary>
public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<User> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Zone> Zones { get; }

    DbSet<Property> Properties { get; }

    DbSet<PropertyTranslation> PropertyTranslations { get; }

    DbSet<MediaFile> MediaFiles { get; }

    DbSet<Lead> Leads { get; }

    DbSet<Owner> Owners { get; }

    DbSet<Visit> Visits { get; }

    DbSet<AnalyticsEvent> AnalyticsEvents { get; }

    DbSet<SiteSettings> SiteSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ITenantContext
{
    /// <summary>Tenant do token (admin) ou do site configurado (visitante). <see cref="Guid.Empty"/> quando não há.</summary>
    Guid TenantId { get; }
}
