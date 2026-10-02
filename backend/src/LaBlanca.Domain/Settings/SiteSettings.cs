using LaBlanca.Domain.Common;

namespace LaBlanca.Domain.Settings;

public sealed class SiteSettings : TenantEntity, IAuditable
{
    private SiteSettings()
    {
    }

    public string CompanyName { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? WhatsappNumber { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    public double? OfficeLatitude { get; private set; }

    public double? OfficeLongitude { get; private set; }

    public string? OpeningHours { get; private set; }

    public string? FacebookUrl { get; private set; }

    public string? InstagramUrl { get; private set; }

    public string? TiktokUrl { get; private set; }

    public string? YoutubeUrl { get; private set; }

    public decimal PygPerUsd { get; private set; }

    public decimal BrlPerUsd { get; private set; }

    public DateTimeOffset RatesUpdatedAt { get; private set; }

    public decimal SimulatorAnnualRate { get; private set; }

    public int MonthlySalesGoal { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static SiteSettings CreateDefault(Guid tenantId, string companyName, DateTimeOffset now) => new()
    {
        TenantId = tenantId,
        CompanyName = companyName,
        PygPerUsd = 7500m,
        BrlPerUsd = 5m,
        RatesUpdatedAt = now,
        SimulatorAnnualRate = 8m,
        MonthlySalesGoal = 10,
    };

    public void UpdateContact(SiteContact contact)
    {
        CompanyName = contact.CompanyName.Trim();
        Phone = Clean(contact.Phone);
        WhatsappNumber = Clean(contact.WhatsappNumber);
        Email = Clean(contact.Email)?.ToLowerInvariant();
        Address = Clean(contact.Address);
        OfficeLatitude = contact.OfficeLatitude;
        OfficeLongitude = contact.OfficeLongitude;
        OpeningHours = Clean(contact.OpeningHours);
        FacebookUrl = Clean(contact.FacebookUrl);
        InstagramUrl = Clean(contact.InstagramUrl);
        TiktokUrl = Clean(contact.TiktokUrl);
        YoutubeUrl = Clean(contact.YoutubeUrl);
    }

    public void UpdateRates(decimal pygPerUsd, decimal brlPerUsd, DateTimeOffset now)
    {
        if (pygPerUsd != PygPerUsd || brlPerUsd != BrlPerUsd)
        {
            RatesUpdatedAt = now;
        }

        PygPerUsd = pygPerUsd;
        BrlPerUsd = brlPerUsd;
    }

    public void UpdateBusiness(decimal simulatorAnnualRate, int monthlySalesGoal)
    {
        SimulatorAnnualRate = simulatorAnnualRate;
        MonthlySalesGoal = monthlySalesGoal;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record SiteContact(
    string CompanyName,
    string? Phone,
    string? WhatsappNumber,
    string? Email,
    string? Address,
    double? OfficeLatitude,
    double? OfficeLongitude,
    string? OpeningHours,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TiktokUrl,
    string? YoutubeUrl);
