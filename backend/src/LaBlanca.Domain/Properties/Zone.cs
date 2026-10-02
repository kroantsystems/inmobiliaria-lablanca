using LaBlanca.Domain.Common;

namespace LaBlanca.Domain.Properties;

public sealed class Zone : TenantEntity
{
    private readonly List<ZoneTranslation> _translations = [];

    private Zone()
    {
    }

    public string Slug { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<ZoneTranslation> Translations => _translations;

    public static Zone Create(string slug, string city, int sortOrder) => new() { Slug = slug, City = city.Trim(), SortOrder = sortOrder };

    public void Update(string slug, string city, int sortOrder)
    {
        Slug = slug;
        City = city.Trim();
        SortOrder = sortOrder;
    }

    public ZoneTranslation? TranslationFor(string locale) => _translations.FirstOrDefault(t => t.Locale == locale);

    public void SetTranslation(string locale, string name, string? description)
    {
        var existing = TranslationFor(locale);
        if (existing is null)
        {
            _translations.Add(new ZoneTranslation(Id, locale, name, description));
        }
        else
        {
            existing.Update(name, description);
        }
    }

    public void RemoveTranslation(string locale) => _translations.RemoveAll(t => t.Locale == locale);
}

public sealed class ZoneTranslation : TenantEntity
{
    private ZoneTranslation()
    {
    }

    internal ZoneTranslation(Guid zoneId, string locale, string name, string? description)
    {
        ZoneId = zoneId;
        Locale = locale;
        Update(name, description);
    }

    public Guid ZoneId { get; private set; }

    public string Locale { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    internal void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
