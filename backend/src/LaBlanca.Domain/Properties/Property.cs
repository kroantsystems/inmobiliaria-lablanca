using LaBlanca.Domain.Common;
using LaBlanca.Domain.Media;

namespace LaBlanca.Domain.Properties;

public sealed class Property : TenantEntity, IAuditable
{
    private readonly List<PropertyTranslation> _translations = [];
    private readonly List<MediaFile> _media = [];

    private Property()
    {
    }

    public PropertyOperation Operation { get; private set; }

    public PropertyType Type { get; private set; }

    public PropertyStatus Status { get; private set; } = PropertyStatus.Draft;

    public bool IsPublished { get; private set; }

    public bool IsFeatured { get; private set; }

    public decimal Price { get; private set; }

    public Currency Currency { get; private set; }

    public Guid ZoneId { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string? Address { get; private set; }

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public int? Bedrooms { get; private set; }

    public int? Bathrooms { get; private set; }

    public decimal? BuiltAreaM2 { get; private set; }

    public decimal? LotAreaM2 { get; private set; }

    public int? ParkingSpaces { get; private set; }

    public List<string> Features { get; private set; } = [];

    public string? VideoUrl { get; private set; }

    public Guid? OwnerId { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? SoldAt { get; private set; }

    public DateTimeOffset? RentedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<PropertyTranslation> Translations => _translations;

    public IReadOnlyCollection<MediaFile> Media => _media;

    public bool HasPublicImage => _media.Any(m => m.Kind == MediaKind.Image && m.IsPublic);

    public static Property Create(PropertyOperation operation, PropertyType type, decimal price, Currency currency, Guid zoneId) => new()
    {
        Operation = operation,
        Type = type,
        Price = price,
        Currency = currency,
        ZoneId = zoneId,
    };

    public void UpdateDetails(PropertyDetails details)
    {
        Operation = details.Operation;
        Type = details.Type;
        Price = details.Price;
        Currency = details.Currency;
        ZoneId = details.ZoneId;
        City = details.City.Trim();
        Address = details.Address?.Trim();
        Latitude = details.Latitude;
        Longitude = details.Longitude;
        Bedrooms = details.Bedrooms;
        Bathrooms = details.Bathrooms;
        BuiltAreaM2 = details.BuiltAreaM2;
        LotAreaM2 = details.LotAreaM2;
        ParkingSpaces = details.ParkingSpaces;
        Features = [.. details.Features.Select(f => f.Trim()).Where(f => f.Length > 0).Distinct()];
        VideoUrl = string.IsNullOrWhiteSpace(details.VideoUrl) ? null : details.VideoUrl.Trim();
        OwnerId = details.OwnerId;
    }

    public PropertyTranslation? TranslationFor(string locale) => _translations.FirstOrDefault(t => t.Locale == locale);

    public void SetTranslation(string locale, string title, string slug, string? description, string? seoTitle, string? seoDescription)
    {
        var existing = TranslationFor(locale);
        if (existing is null)
        {
            _translations.Add(new PropertyTranslation(Id, locale, title, slug, description, seoTitle, seoDescription));
        }
        else
        {
            existing.Update(title, slug, description, seoTitle, seoDescription);
        }
    }

    public void RemoveTranslation(string locale) => _translations.RemoveAll(t => t.Locale == locale);

    public void Publish(DateTimeOffset now)
    {
        if (!PropertyStatuses.IsListed(Status))
        {
            throw new DomainException(DomainErrors.PropertyMustBeAvailableToPublish);
        }

        if (!HasPublicImage)
        {
            throw new DomainException(DomainErrors.PropertyPublishRequiresImage);
        }

        IsPublished = true;
        PublishedAt = now;
    }

    public void Unpublish()
    {
        IsPublished = false;
        IsFeatured = false;
    }

    public void SetFeatured(bool featured)
    {
        if (featured && (!IsPublished || !PropertyStatuses.IsListed(Status)))
        {
            throw new DomainException(DomainErrors.PropertyMustBePublishedToFeature);
        }

        IsFeatured = featured;
    }

    public void ChangeStatus(PropertyStatus status, DateTimeOffset now)
    {
        if (status == Status)
        {
            return;
        }

        Status = status;
        SoldAt = status == PropertyStatus.Sold ? now : null;
        RentedAt = status == PropertyStatus.Rented ? now : null;

        if (!PropertyStatuses.IsListed(status))
        {
            IsFeatured = false;
        }

        if (status is PropertyStatus.Archived or PropertyStatus.Draft)
        {
            IsPublished = false;
        }
    }

    public void AddMedia(MediaFile media)
    {
        media.LinkTo(Id);
        media.SetSortOrder(_media.Count == 0 ? 0 : _media.Max(m => m.SortOrder) + 1);
        _media.Add(media);
    }

    public void SetCover(Guid mediaId)
    {
        var cover = _media.FirstOrDefault(m => m.Id == mediaId) ?? throw new DomainException(DomainErrors.MediaNotInProperty);
        cover.MarkAsCover();
        foreach (var other in _media.Where(m => m.Id != mediaId))
        {
            other.UnmarkCover();
        }
    }

    public void ReorderMedia(IReadOnlyList<Guid> orderedIds)
    {
        if (orderedIds.Any(id => _media.All(m => m.Id != id)))
        {
            throw new DomainException(DomainErrors.MediaNotInProperty);
        }

        for (var i = 0; i < orderedIds.Count; i++)
        {
            _media.First(m => m.Id == orderedIds[i]).SetSortOrder(i);
        }
    }

    /// <summary>Mídia pública na ordem de exibição: capa primeiro, depois a ordem definida pelo admin.</summary>
    public IReadOnlyList<MediaFile> Gallery() =>
    [
        .. _media
            .Where(m => m.IsPublic && m.Kind != MediaKind.Document)
            .OrderByDescending(m => m.IsCover)
            .ThenBy(m => m.SortOrder),
    ];
}

public sealed record PropertyDetails(
    PropertyOperation Operation,
    PropertyType Type,
    decimal Price,
    Currency Currency,
    Guid ZoneId,
    string City,
    string? Address,
    double? Latitude,
    double? Longitude,
    int? Bedrooms,
    int? Bathrooms,
    decimal? BuiltAreaM2,
    decimal? LotAreaM2,
    int? ParkingSpaces,
    IReadOnlyList<string> Features,
    string? VideoUrl,
    Guid? OwnerId);

public sealed class PropertyTranslation : TenantEntity
{
    private PropertyTranslation()
    {
    }

    internal PropertyTranslation(Guid propertyId, string locale, string title, string slug, string? description, string? seoTitle, string? seoDescription)
    {
        PropertyId = propertyId;
        Locale = locale;
        Update(title, slug, description, seoTitle, seoDescription);
    }

    public Guid PropertyId { get; private set; }

    public string Locale { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? SeoTitle { get; private set; }

    public string? SeoDescription { get; private set; }

    internal void Update(string title, string slug, string? description, string? seoTitle, string? seoDescription)
    {
        Title = title.Trim();
        Slug = slug;
        Description = description?.Trim();
        SeoTitle = string.IsNullOrWhiteSpace(seoTitle) ? null : seoTitle.Trim();
        SeoDescription = string.IsNullOrWhiteSpace(seoDescription) ? null : seoDescription.Trim();
    }
}
