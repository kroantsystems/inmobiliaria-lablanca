using System.Linq.Expressions;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Common;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Media;
using LaBlanca.Domain.Properties;
using LaBlanca.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Properties;

public enum PropertySort
{
    Recent,
    PriceAsc,
    PriceDesc,
}

public sealed record SearchPublicPropertiesQuery(
    string? Locale = null,
    PropertyOperation? Operation = null,
    PropertyType? Type = null,
    string? Zone = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinBedrooms = null,
    string? Q = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = SearchPublicPropertiesQuery.MaxPageSize) : IQuery<PagedResult<PublicPropertyCard>>
{
    public const int MaxPageSize = 24;

    public PropertySort SortOrder => Sort switch
    {
        "price_asc" => PropertySort.PriceAsc,
        "price_desc" => PropertySort.PriceDesc,
        _ => PropertySort.Recent,
    };
}

internal sealed class PublicCatalog(IAppDbContext db)
{
    /// <summary>Somente publicados e disponíveis/reservados.</summary>
    public IQueryable<Property> Listed() =>
        db.Properties.AsNoTracking().Where(p => p.IsPublished && PropertyStatuses.Listed.Contains(p.Status));

    public async Task<Expression<Func<Property, decimal>>> PriceInUsdAsync(CancellationToken cancellationToken)
    {
        var rates = await db.SiteSettings.AsNoTracking().Select(s => new { s.PygPerUsd, s.BrlPerUsd }).FirstOrDefaultAsync(cancellationToken);
        var pyg = rates?.PygPerUsd ?? 1m;
        var brl = rates?.BrlPerUsd ?? 1m;
        return p => p.Currency == Currency.USD ? p.Price : p.Currency == Currency.PYG ? p.Price / pyg : p.Price / brl;
    }

    public async Task<List<Property>> LoadAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var loaded = await db.Properties.AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Media.Where(m => m.IsPublic && m.Kind != MediaKind.Document))
            .AsSplitQuery()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
        return [.. ids.Select(id => loaded.First(p => p.Id == id))];
    }

    public async Task<IReadOnlyDictionary<Guid, PublicZoneRef>> ZonesAsync(string locale, CancellationToken cancellationToken)
    {
        var zones = await db.Zones.AsNoTracking().Include(z => z.Translations).ToListAsync(cancellationToken);
        return zones.Where(z => z.Translations.Count > 0).ToDictionary(
            z => z.Id,
            z => new PublicZoneRef(z.Slug, (z.TranslationFor(locale) ?? z.TranslationFor(Locales.Default) ?? z.Translations.First()).Name));
    }

    public async Task<List<PublicPropertyCard>> CardsAsync(IReadOnlyCollection<Guid> ids, string locale, CancellationToken cancellationToken)
    {
        var zones = await ZonesAsync(locale, cancellationToken);
        return [.. (await LoadAsync(ids, cancellationToken)).Select(p => p.ToCard(locale, zones))];
    }
}

public sealed class SearchPublicPropertiesQueryHandler(IAppDbContext db) : IRequestHandler<SearchPublicPropertiesQuery, PagedResult<PublicPropertyCard>>
{
    public async Task<PagedResult<PublicPropertyCard>> Handle(SearchPublicPropertiesQuery request, CancellationToken cancellationToken)
    {
        var catalog = new PublicCatalog(db);
        var locale = ContentLocale.Resolve(request.Locale);
        var pageSize = Math.Clamp(request.PageSize, 1, SearchPublicPropertiesQuery.MaxPageSize);
        var page = Math.Max(request.Page, 1);

        var query = catalog.Listed();
        if (request.Operation is { } operation)
        {
            query = query.Where(p => p.Operation == operation);
        }

        if (request.Type is { } type)
        {
            query = query.Where(p => p.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Zone))
        {
            query = query.Where(p => db.Zones.Any(z => z.Id == p.ZoneId && z.Slug == request.Zone));
        }

        if (request.MinBedrooms is { } bedrooms)
        {
            query = query.Where(p => p.Bedrooms >= bedrooms);
        }

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var term = request.Q.Trim().ToLower();
            query = query.Where(p => p.City.ToLower().Contains(term)
                || p.Translations.Any(t => t.Title.ToLower().Contains(term) || (t.Description != null && t.Description.ToLower().Contains(term))));
        }

        var priceInUsd = await catalog.PriceInUsdAsync(cancellationToken);
        var withPrice = query.Select(Project(priceInUsd));
        if (request.MinPrice is { } min)
        {
            withPrice = withPrice.Where(x => x.UsdPrice >= min);
        }

        if (request.MaxPrice is { } max)
        {
            withPrice = withPrice.Where(x => x.UsdPrice <= max);
        }

        withPrice = request.SortOrder switch
        {
            PropertySort.PriceAsc => withPrice.OrderBy(x => x.UsdPrice).ThenByDescending(x => x.PublishedAt),
            PropertySort.PriceDesc => withPrice.OrderByDescending(x => x.UsdPrice).ThenByDescending(x => x.PublishedAt),
            _ => withPrice.OrderByDescending(x => x.PublishedAt),
        };

        var total = await withPrice.CountAsync(cancellationToken);
        var ids = await withPrice.Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(cancellationToken);
        return new PagedResult<PublicPropertyCard>(await catalog.CardsAsync(ids, locale, cancellationToken), total, page, pageSize);
    }

    private static Expression<Func<Property, PricedProperty>> Project(Expression<Func<Property, decimal>> priceInUsd)
    {
        var parameter = priceInUsd.Parameters[0];
        var body = Expression.MemberInit(
            Expression.New(typeof(PricedProperty)),
            Expression.Bind(typeof(PricedProperty).GetProperty(nameof(PricedProperty.Id))!, Expression.Property(parameter, nameof(Property.Id))),
            Expression.Bind(typeof(PricedProperty).GetProperty(nameof(PricedProperty.PublishedAt))!, Expression.Property(parameter, nameof(Property.PublishedAt))),
            Expression.Bind(typeof(PricedProperty).GetProperty(nameof(PricedProperty.UsdPrice))!, priceInUsd.Body));
        return Expression.Lambda<Func<Property, PricedProperty>>(body, parameter);
    }

    private sealed class PricedProperty
    {
        public Guid Id { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public decimal UsdPrice { get; init; }
    }
}

public sealed record GetFeaturedPropertiesQuery(string? Locale = null, int Take = 6) : IQuery<IReadOnlyList<PublicPropertyCard>>;

public sealed class GetFeaturedPropertiesQueryHandler(IAppDbContext db) : IRequestHandler<GetFeaturedPropertiesQuery, IReadOnlyList<PublicPropertyCard>>
{
    public async Task<IReadOnlyList<PublicPropertyCard>> Handle(GetFeaturedPropertiesQuery request, CancellationToken cancellationToken)
    {
        var catalog = new PublicCatalog(db);
        var take = Math.Clamp(request.Take, 1, 12);
        var ids = await catalog.Listed().Where(p => p.IsFeatured).OrderByDescending(p => p.PublishedAt).Take(take).Select(p => p.Id).ToListAsync(cancellationToken);
        if (ids.Count == 0)
        {
            ids = await catalog.Listed().OrderByDescending(p => p.PublishedAt).Take(take).Select(p => p.Id).ToListAsync(cancellationToken);
        }

        return await catalog.CardsAsync(ids, ContentLocale.Resolve(request.Locale), cancellationToken);
    }
}

public sealed record GetPublicMapQuery(string? Locale = null) : IQuery<IReadOnlyList<PublicPropertyCard>>;

public sealed class GetPublicMapQueryHandler(IAppDbContext db) : IRequestHandler<GetPublicMapQuery, IReadOnlyList<PublicPropertyCard>>
{
    public async Task<IReadOnlyList<PublicPropertyCard>> Handle(GetPublicMapQuery request, CancellationToken cancellationToken)
    {
        var catalog = new PublicCatalog(db);
        var ids = await catalog.Listed().Where(p => p.Latitude != null && p.Longitude != null)
            .OrderByDescending(p => p.PublishedAt).Take(500).Select(p => p.Id).ToListAsync(cancellationToken);
        return await catalog.CardsAsync(ids, ContentLocale.Resolve(request.Locale), cancellationToken);
    }
}

public sealed record GetPublicPropertyQuery(string Locale, string Slug) : IQuery<PublicPropertyDetail>;

public sealed class GetPublicPropertyQueryHandler(IAppDbContext db) : IRequestHandler<GetPublicPropertyQuery, PublicPropertyDetail>
{
    public async Task<PublicPropertyDetail> Handle(GetPublicPropertyQuery request, CancellationToken cancellationToken)
    {
        var catalog = new PublicCatalog(db);
        var locale = ContentLocale.Resolve(request.Locale);
        var matches = await db.PropertyTranslations.AsNoTracking()
            .Where(t => t.Slug == request.Slug && (t.Locale == locale || t.Locale == Locales.Default))
            .Select(t => new { t.PropertyId, t.Locale })
            .ToListAsync(cancellationToken);
        var match = matches.FirstOrDefault(m => m.Locale == locale) ?? matches.FirstOrDefault();
        if (match is null || !await catalog.Listed().AnyAsync(p => p.Id == match.PropertyId, cancellationToken))
        {
            throw new NotFoundException();
        }

        var property = (await catalog.LoadAsync([match.PropertyId], cancellationToken))[0];
        var zones = await catalog.ZonesAsync(locale, cancellationToken);
        var t = property.Translation(locale);
        return new PublicPropertyDetail(
            property.Id, t.Slug, t.Title, t.Locale, t.Description, t.SeoTitle, t.SeoDescription, property.Operation, property.Type,
            property.Status, property.Price, property.Currency, zones.GetValueOrDefault(property.ZoneId), property.City, property.Bedrooms,
            property.Bathrooms, property.BuiltAreaM2, property.LotAreaM2, property.ParkingSpaces, property.Features, property.VideoUrl,
            property.Latitude, property.Longitude, [.. property.Gallery().Select(m => m.ToPublicImage(t.Title))],
            property.SlugsByLocale(), property.PublishedAt, property.UpdatedAt);
    }
}

public sealed record GetSimilarPropertiesQuery(Guid Id, string? Locale = null, int Take = 3) : IQuery<IReadOnlyList<PublicPropertyCard>>;

public sealed class GetSimilarPropertiesQueryHandler(IAppDbContext db) : IRequestHandler<GetSimilarPropertiesQuery, IReadOnlyList<PublicPropertyCard>>
{
    public async Task<IReadOnlyList<PublicPropertyCard>> Handle(GetSimilarPropertiesQuery request, CancellationToken cancellationToken)
    {
        var catalog = new PublicCatalog(db);
        var reference = await db.Properties.AsNoTracking().Where(p => p.Id == request.Id)
            .Select(p => new { p.Operation, p.Type, p.ZoneId }).FirstOrDefaultAsync(cancellationToken);
        if (reference is null)
        {
            return [];
        }

        var ids = await catalog.Listed()
            .Where(p => p.Id != request.Id && p.Operation == reference.Operation)
            .OrderByDescending(p => p.ZoneId == reference.ZoneId)
            .ThenByDescending(p => p.Type == reference.Type)
            .ThenByDescending(p => p.PublishedAt)
            .Take(Math.Clamp(request.Take, 1, 6))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        return await catalog.CardsAsync(ids, ContentLocale.Resolve(request.Locale), cancellationToken);
    }
}

public sealed record GetSitemapPropertiesQuery : IQuery<IReadOnlyList<SitemapPropertyEntry>>;

public sealed class GetSitemapPropertiesQueryHandler(IAppDbContext db) : IRequestHandler<GetSitemapPropertiesQuery, IReadOnlyList<SitemapPropertyEntry>>
{
    public async Task<IReadOnlyList<SitemapPropertyEntry>> Handle(GetSitemapPropertiesQuery request, CancellationToken cancellationToken)
    {
        var catalog = new PublicCatalog(db);
        var properties = await catalog.Listed().Include(p => p.Translations).OrderByDescending(p => p.PublishedAt).ToListAsync(cancellationToken);
        var zoneSlugs = await db.Zones.AsNoTracking().ToDictionaryAsync(z => z.Id, z => z.Slug, cancellationToken);
        return
        [
            .. properties.Select(p => new SitemapPropertyEntry(
                p.Id,
                p.SlugsByLocale(),
                Locales.All.ToDictionary(l => l, l => p.Translation(l).Title),
                zoneSlugs.GetValueOrDefault(p.ZoneId),
                p.Operation,
                p.Type,
                p.Price,
                p.Currency,
                p.City,
                p.UpdatedAt ?? p.PublishedAt ?? p.CreatedAt)),
        ];
    }
}
