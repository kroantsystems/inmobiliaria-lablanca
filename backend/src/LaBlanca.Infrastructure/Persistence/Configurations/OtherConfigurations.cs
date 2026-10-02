using LaBlanca.Domain.Analytics;
using LaBlanca.Domain.Leads;
using LaBlanca.Domain.Owners;
using LaBlanca.Domain.Properties;
using LaBlanca.Domain.Settings;
using LaBlanca.Domain.Visits;
using LaBlanca.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaBlanca.Infrastructure.Persistence.Configurations;

internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.Property(l => l.Name).HasMaxLength(120).IsRequired();
        builder.Property(l => l.Phone).HasMaxLength(32).IsRequired();
        builder.Property(l => l.Email).HasMaxLength(254);
        builder.Property(l => l.Message).HasMaxLength(2000);
        builder.Property(l => l.Notes).HasMaxLength(2000);
        builder.Property(l => l.Locale).HasMaxLength(5).IsRequired();
        builder.HasIndex(l => new { l.TenantId, l.CreatedAt });
        builder.HasOne<Property>().WithMany().HasForeignKey(l => l.PropertyId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.Property(o => o.Name).HasMaxLength(120).IsRequired();
        builder.Property(o => o.Phone).HasMaxLength(32).IsRequired();
        builder.Property(o => o.Email).HasMaxLength(254);
        builder.Property(o => o.Document).HasMaxLength(40);
        builder.Property(o => o.Notes).HasMaxLength(2000);
    }
}

internal sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.Property(v => v.ClientName).HasMaxLength(120).IsRequired();
        builder.Property(v => v.Notes).HasMaxLength(1000);
        builder.HasIndex(v => new { v.TenantId, v.StartsAt });
        builder.HasOne<Property>().WithMany().HasForeignKey(v => v.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Lead>().WithMany().HasForeignKey(v => v.LeadId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class AnalyticsEventConfiguration : IEntityTypeConfiguration<AnalyticsEvent>
{
    public void Configure(EntityTypeBuilder<AnalyticsEvent> builder)
    {
        builder.Property(e => e.Path).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Locale).HasMaxLength(5).IsRequired();
        builder.Property(e => e.ReferrerHost).HasMaxLength(255);
        builder.HasIndex(e => new { e.TenantId, e.OccurredAt, e.Type });
        builder.HasIndex(e => new { e.TenantId, e.PropertyId, e.OccurredAt });
        builder.HasOne<Property>().WithMany().HasForeignKey(e => e.PropertyId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.Property(s => s.CompanyName).HasMaxLength(120).IsRequired();
        builder.Property(s => s.Phone).HasMaxLength(32);
        builder.Property(s => s.WhatsappNumber).HasMaxLength(20);
        builder.Property(s => s.Email).HasMaxLength(254);
        builder.Property(s => s.Address).HasMaxLength(200);
        builder.Property(s => s.OpeningHours).HasMaxLength(200);
        builder.Property(s => s.FacebookUrl).HasMaxLength(300);
        builder.Property(s => s.InstagramUrl).HasMaxLength(300);
        builder.Property(s => s.TiktokUrl).HasMaxLength(300);
        builder.Property(s => s.YoutubeUrl).HasMaxLength(300);
        builder.Property(s => s.PygPerUsd).HasPrecision(18, 4);
        builder.Property(s => s.BrlPerUsd).HasPrecision(18, 4);
        builder.Property(s => s.SimulatorAnnualRate).HasPrecision(5, 2);
        builder.HasIndex(s => s.TenantId).IsUnique();
        builder.HasData(SeedData.Settings);
    }
}
