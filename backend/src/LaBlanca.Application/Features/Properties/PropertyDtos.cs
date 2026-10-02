using LaBlanca.Domain.Common;
using LaBlanca.Domain.Media;
using LaBlanca.Domain.Properties;

namespace LaBlanca.Application.Features.Properties;

public static class MediaUrls
{
    public static string Public(Guid mediaId) => $"/api/public/media/{mediaId}";

    public static string Admin(Guid mediaId) => $"/api/admin/files/{mediaId}/content";
}

public sealed record PropertyTranslationInput(string Locale, string Title, string? Description, string? SeoTitle, string? SeoDescription);

public record PropertyInput(
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
    IReadOnlyList<string>? Features,
    string? VideoUrl,
    Guid? OwnerId,
    IReadOnlyList<PropertyTranslationInput> Translations);

public sealed record PropertyTranslationDto(string Locale, string Title, string Slug, string? Description, string? SeoTitle, string? SeoDescription);

public sealed record AdminMediaDto(
    Guid Id,
    MediaKind Kind,
    string OriginalName,
    string ContentType,
    long SizeBytes,
    string Url,
    string? PublicUrl,
    bool IsPublic,
    bool IsCover,
    int SortOrder,
    string? AltText,
    string? Description);

public sealed record AdminPropertyDto(
    Guid Id,
    PropertyOperation Operation,
    PropertyType Type,
    PropertyStatus Status,
    bool IsPublished,
    bool IsFeatured,
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
    Guid? OwnerId,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? SoldAt,
    DateTimeOffset? RentedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<PropertyTranslationDto> Translations,
    IReadOnlyList<AdminMediaDto> Media);

public sealed record AdminPropertyListItem(
    Guid Id,
    string Title,
    string Slug,
    PropertyOperation Operation,
    PropertyType Type,
    PropertyStatus Status,
    bool IsPublished,
    bool IsFeatured,
    decimal Price,
    Currency Currency,
    Guid ZoneId,
    string? ZoneName,
    string City,
    int? Bedrooms,
    int? Bathrooms,
    decimal? BuiltAreaM2,
    string? CoverUrl,
    int MediaCount,
    Guid? OwnerId,
    string? OwnerName,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record PublicZoneRef(string Slug, string Name);

public sealed record PublicImageDto(string Url, string Alt, MediaKind Kind, string ContentType);

public sealed record PublicPropertyCard(
    Guid Id,
    string Slug,
    string Title,
    string Locale,
    PropertyOperation Operation,
    PropertyType Type,
    PropertyStatus Status,
    decimal Price,
    Currency Currency,
    PublicZoneRef? Zone,
    string City,
    int? Bedrooms,
    int? Bathrooms,
    decimal? BuiltAreaM2,
    decimal? LotAreaM2,
    double? Latitude,
    double? Longitude,
    PublicImageDto? Cover,
    bool IsFeatured,
    DateTimeOffset? PublishedAt);

public sealed record PublicPropertyDetail(
    Guid Id,
    string Slug,
    string Title,
    string Locale,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    PropertyOperation Operation,
    PropertyType Type,
    PropertyStatus Status,
    decimal Price,
    Currency Currency,
    PublicZoneRef? Zone,
    string City,
    int? Bedrooms,
    int? Bathrooms,
    decimal? BuiltAreaM2,
    decimal? LotAreaM2,
    int? ParkingSpaces,
    IReadOnlyList<string> Features,
    string? VideoUrl,
    double? Latitude,
    double? Longitude,
    IReadOnlyList<PublicImageDto> Gallery,
    IReadOnlyDictionary<string, string> Slugs,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UpdatedAt);

public sealed record SitemapPropertyEntry(
    Guid Id,
    IReadOnlyDictionary<string, string> Slugs,
    IReadOnlyDictionary<string, string> Titles,
    string? ZoneSlug,
    PropertyOperation Operation,
    PropertyType Type,
    decimal Price,
    Currency Currency,
    string City,
    DateTimeOffset LastModified);

internal static class PropertyMapping
{
    public static PropertyTranslation Translation(this Property property, string locale) =>
        property.TranslationFor(locale) ?? property.TranslationFor(Locales.Default) ?? property.Translations.First();

    public static IReadOnlyDictionary<string, string> SlugsByLocale(this Property property)
    {
        var fallback = property.Translation(Locales.Default).Slug;
        return Locales.All.ToDictionary(l => l, l => property.TranslationFor(l)?.Slug ?? fallback);
    }

    public static PublicImageDto ToPublicImage(this MediaFile media, string fallbackAlt) =>
        new(MediaUrls.Public(media.Id), media.AltText ?? fallbackAlt, media.Kind, media.ContentType);

    public static PublicPropertyCard ToCard(this Property p, string locale, IReadOnlyDictionary<Guid, PublicZoneRef> zones)
    {
        var t = p.Translation(locale);
        var cover = p.Gallery().FirstOrDefault(m => m.Kind == MediaKind.Image);
        return new PublicPropertyCard(
            p.Id, t.Slug, t.Title, t.Locale, p.Operation, p.Type, p.Status, p.Price, p.Currency, zones.GetValueOrDefault(p.ZoneId),
            p.City, p.Bedrooms, p.Bathrooms, p.BuiltAreaM2, p.LotAreaM2, p.Latitude, p.Longitude, cover?.ToPublicImage(t.Title),
            p.IsFeatured, p.PublishedAt);
    }

    public static AdminPropertyDto ToAdminDto(this Property p) => new(
        p.Id, p.Operation, p.Type, p.Status, p.IsPublished, p.IsFeatured, p.Price, p.Currency, p.ZoneId, p.City, p.Address,
        p.Latitude, p.Longitude, p.Bedrooms, p.Bathrooms, p.BuiltAreaM2, p.LotAreaM2, p.ParkingSpaces, p.Features, p.VideoUrl,
        p.OwnerId, p.PublishedAt, p.SoldAt, p.RentedAt, p.CreatedAt, p.UpdatedAt,
        [.. p.Translations.OrderBy(t => Locales.All.ToList().IndexOf(t.Locale))
            .Select(t => new PropertyTranslationDto(t.Locale, t.Title, t.Slug, t.Description, t.SeoTitle, t.SeoDescription))],
        [.. p.Media.OrderByDescending(m => m.IsCover).ThenBy(m => m.SortOrder).Select(m => m.ToAdminMediaDto())]);

    public static AdminMediaDto ToAdminMediaDto(this MediaFile m) => new(
        m.Id, m.Kind, m.OriginalName, m.ContentType, m.SizeBytes, MediaUrls.Admin(m.Id), m.IsPublic ? MediaUrls.Public(m.Id) : null,
        m.IsPublic, m.IsCover, m.SortOrder, m.AltText, m.Description);
}
