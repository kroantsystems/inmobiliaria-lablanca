using LaBlanca.Domain.Media;
using LaBlanca.Domain.Owners;
using LaBlanca.Domain.Properties;
using LaBlanca.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaBlanca.Infrastructure.Persistence.Configurations;

internal sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.Property(z => z.Slug).HasMaxLength(80).IsRequired();
        builder.Property(z => z.City).HasMaxLength(80).IsRequired();
        builder.HasIndex(z => new { z.TenantId, z.Slug }).IsUnique();
        builder.HasMany(z => z.Translations).WithOne().HasForeignKey(t => t.ZoneId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(z => z.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasData(SeedData.Zones);
    }
}

internal sealed class ZoneTranslationConfiguration : IEntityTypeConfiguration<ZoneTranslation>
{
    public void Configure(EntityTypeBuilder<ZoneTranslation> builder)
    {
        builder.ToTable("ZoneTranslations");
        builder.Property(t => t.Locale).HasMaxLength(5).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(120).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.HasIndex(t => new { t.ZoneId, t.Locale }).IsUnique();
        builder.HasData(SeedData.ZoneTranslations);
    }
}

internal sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.City).HasMaxLength(80).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(200);
        builder.Property(p => p.BuiltAreaM2).HasPrecision(12, 2);
        builder.Property(p => p.LotAreaM2).HasPrecision(12, 2);
        builder.Property(p => p.VideoUrl).HasMaxLength(500);

        builder.HasIndex(p => new { p.TenantId, p.IsPublished, p.Status, p.Operation, p.Type, p.ZoneId, p.Price });
        builder.HasIndex(p => new { p.TenantId, p.IsFeatured, p.PublishedAt });

        builder.HasOne<Zone>().WithMany().HasForeignKey(p => p.ZoneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Owner>().WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Translations).WithOne().HasForeignKey(t => t.PropertyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Media).WithOne().HasForeignKey(m => m.PropertyId).OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(p => p.Media).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PropertyTranslationConfiguration : IEntityTypeConfiguration<PropertyTranslation>
{
    public void Configure(EntityTypeBuilder<PropertyTranslation> builder)
    {
        builder.Property(t => t.Locale).HasMaxLength(5).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(160).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(90).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(8000);
        builder.Property(t => t.SeoTitle).HasMaxLength(70);
        builder.Property(t => t.SeoDescription).HasMaxLength(170);
        builder.HasIndex(t => new { t.PropertyId, t.Locale }).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.Locale, t.Slug }).IsUnique();
    }
}

internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.Property(m => m.OriginalName).HasMaxLength(255).IsRequired();
        builder.Property(m => m.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(m => m.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(500);
        builder.Property(m => m.AltText).HasMaxLength(200);
        builder.HasIndex(m => m.StorageKey).IsUnique();
        builder.HasIndex(m => new { m.TenantId, m.PropertyId });
    }
}
