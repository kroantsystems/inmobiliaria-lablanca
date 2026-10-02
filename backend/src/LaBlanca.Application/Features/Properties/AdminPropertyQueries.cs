using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Media;
using LaBlanca.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Properties;

public sealed record GetAdminPropertyQuery(Guid Id) : IQuery<AdminPropertyDto>;

public sealed class GetAdminPropertyQueryHandler(IAppDbContext db) : IRequestHandler<GetAdminPropertyQuery, AdminPropertyDto>
{
    public async Task<AdminPropertyDto> Handle(GetAdminPropertyQuery request, CancellationToken cancellationToken)
    {
        var property = await db.Properties.AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Media)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException();
        return property.ToAdminDto();
    }
}

/// <summary>Página ampla para o painel filtrar e ordenar no cliente.</summary>
public sealed record GetAdminPropertiesQuery(int Page = 1, int PageSize = GetAdminPropertiesQuery.MaxPageSize) : IQuery<PagedResult<AdminPropertyListItem>>
{
    public const int MaxPageSize = 500;
}

public sealed class GetAdminPropertiesQueryHandler(IAppDbContext db) : IRequestHandler<GetAdminPropertiesQuery, PagedResult<AdminPropertyListItem>>
{
    public async Task<PagedResult<AdminPropertyListItem>> Handle(GetAdminPropertiesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, GetAdminPropertiesQuery.MaxPageSize);
        var page = Math.Max(request.Page, 1);

        var total = await db.Properties.CountAsync(cancellationToken);
        var properties = await db.Properties.AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Media)
            .AsSplitQuery()
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var zoneNames = await db.Zones.AsNoTracking()
            .SelectMany(z => z.Translations.Where(t => t.Locale == Locales.Default).Select(t => new { z.Id, t.Name }))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var ownerIds = properties.Where(p => p.OwnerId != null).Select(p => p.OwnerId!.Value).Distinct().ToList();
        var ownerNames = await db.Owners.AsNoTracking().Where(o => ownerIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name, cancellationToken);

        var items = properties.Select(p =>
        {
            var t = p.Translation(Locales.Default);
            var cover = p.Media.Where(m => m.Kind == MediaKind.Image).OrderByDescending(m => m.IsCover).ThenBy(m => m.SortOrder).FirstOrDefault();
            return new AdminPropertyListItem(
                p.Id, t.Title, t.Slug, p.Operation, p.Type, p.Status, p.IsPublished, p.IsFeatured, p.Price, p.Currency, p.ZoneId,
                zoneNames.GetValueOrDefault(p.ZoneId), p.City, p.Bedrooms, p.Bathrooms, p.BuiltAreaM2,
                cover is null ? null : MediaUrls.Admin(cover.Id), p.Media.Count, p.OwnerId,
                p.OwnerId is { } ownerId ? ownerNames.GetValueOrDefault(ownerId) : null,
                p.PublishedAt, p.CreatedAt, p.UpdatedAt);
        }).ToList();

        return new PagedResult<AdminPropertyListItem>(items, total, page, pageSize);
    }
}
