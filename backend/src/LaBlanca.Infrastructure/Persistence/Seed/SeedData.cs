using LaBlanca.Domain.Identity;

namespace LaBlanca.Infrastructure.Persistence.Seed;

/// <summary>Dados iniciais com IDs fixos (exigência do HasData). Contatos reais são preenchidos pelo admin.</summary>
public static class SeedData
{
    public static readonly Guid LaBlancaTenantId = Guid.Parse("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71");

    private static readonly DateTimeOffset SeededAt = new(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);

    internal static readonly Tenant Tenant = Tenant.Create(LaBlancaTenantId, "Inmobiliaria La Blanca", "la-blanca");

    internal static readonly object Settings = new
    {
        Id = Guid.Parse("0b6a0d43-6f1e-4a3c-8d52-7c9e1f2a3b40"),
        TenantId = LaBlancaTenantId,
        CompanyName = "Inmobiliaria La Blanca",
        PygPerUsd = 7500m,
        BrlPerUsd = 5m,
        RatesUpdatedAt = SeededAt,
        SimulatorAnnualRate = 8m,
        MonthlySalesGoal = 10,
        CreatedAt = SeededAt,
    };

    private static readonly (Guid Id, string Slug, string City, string Es, string Pt, string En)[] ZoneSeeds =
    [
        (Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e01"), "parana-country-club", "Hernandarias", "Paraná Country Club", "Paraná Country Club", "Paraná Country Club"),
        (Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e02"), "centro-cde", "Ciudad del Este", "Centro de Ciudad del Este", "Centro de Ciudad del Este", "Downtown Ciudad del Este"),
        (Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e03"), "km-8-km-10", "Ciudad del Este", "Km 8 / Km 10", "Km 8 / Km 10", "Km 8 / Km 10"),
        (Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e04"), "area-1-area-4", "Ciudad del Este", "Área 1 / Área 4", "Área 1 / Área 4", "Área 1 / Área 4"),
        (Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05"), "hernandarias", "Hernandarias", "Hernandarias", "Hernandarias", "Hernandarias"),
    ];

    internal static readonly object[] Zones =
    [
        .. ZoneSeeds.Select((z, i) => (object)new { z.Id, TenantId = LaBlancaTenantId, z.Slug, z.City, SortOrder = i + 1 }),
    ];

    internal static readonly object[] ZoneTranslations =
    [
        .. ZoneSeeds.SelectMany((z, i) => new (string Locale, string Name)[] { ("es", z.Es), ("pt", z.Pt), ("en", z.En), ("gn", z.Es) }
            .Select((t, j) => (object)new
            {
                Id = Guid.Parse($"b2e1a4d3-2c5f-4d7b-8a31-{i + 1:D2}{j + 1:D2}00000000"),
                TenantId = LaBlancaTenantId,
                ZoneId = z.Id,
                t.Locale,
                t.Name,
            })),
    ];
}
