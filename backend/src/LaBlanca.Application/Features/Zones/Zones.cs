using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Common;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Properties;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Zones;

public sealed record ZoneTranslationDto(string Locale, string Name, string? Description);

public sealed record AdminZoneDto(Guid Id, string Slug, string City, int SortOrder, IReadOnlyList<ZoneTranslationDto> Translations, int PropertyCount);

public sealed record PublicZoneDto(Guid Id, string Slug, string City, string Name, string? Description, string Locale);

internal static class ZoneMapping
{
    public static PublicZoneDto ToPublicDto(this Zone zone, string locale)
    {
        var translation = zone.TranslationFor(locale) ?? zone.TranslationFor(Locales.Default) ?? zone.Translations.First();
        return new PublicZoneDto(zone.Id, zone.Slug, zone.City, translation.Name, translation.Description, translation.Locale);
    }
}

public sealed record GetPublicZonesQuery(string? Locale) : IQuery<IReadOnlyList<PublicZoneDto>>;

public sealed class GetPublicZonesQueryHandler(IAppDbContext db) : IRequestHandler<GetPublicZonesQuery, IReadOnlyList<PublicZoneDto>>
{
    public async Task<IReadOnlyList<PublicZoneDto>> Handle(GetPublicZonesQuery request, CancellationToken cancellationToken)
    {
        var locale = ContentLocale.Resolve(request.Locale);
        var zones = await db.Zones.AsNoTracking().Include(z => z.Translations).OrderBy(z => z.SortOrder).ThenBy(z => z.Slug).ToListAsync(cancellationToken);
        return [.. zones.Where(z => z.Translations.Count > 0).Select(z => z.ToPublicDto(locale))];
    }
}

public sealed record GetPublicZoneQuery(string Slug, string? Locale) : IQuery<PublicZoneDto>;

public sealed class GetPublicZoneQueryHandler(IAppDbContext db) : IRequestHandler<GetPublicZoneQuery, PublicZoneDto>
{
    public async Task<PublicZoneDto> Handle(GetPublicZoneQuery request, CancellationToken cancellationToken)
    {
        var zone = await db.Zones.AsNoTracking().Include(z => z.Translations).FirstOrDefaultAsync(z => z.Slug == request.Slug, cancellationToken);
        if (zone is null || zone.Translations.Count == 0)
        {
            throw new NotFoundException();
        }

        return zone.ToPublicDto(ContentLocale.Resolve(request.Locale));
    }
}

public sealed record GetAdminZonesQuery : IQuery<IReadOnlyList<AdminZoneDto>>;

public sealed class GetAdminZonesQueryHandler(IAppDbContext db) : IRequestHandler<GetAdminZonesQuery, IReadOnlyList<AdminZoneDto>>
{
    public async Task<IReadOnlyList<AdminZoneDto>> Handle(GetAdminZonesQuery request, CancellationToken cancellationToken)
    {
        var counts = await db.Properties.GroupBy(p => p.ZoneId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var zones = await db.Zones.AsNoTracking().Include(z => z.Translations).OrderBy(z => z.SortOrder).ThenBy(z => z.Slug).ToListAsync(cancellationToken);
        return
        [
            .. zones.Select(z => new AdminZoneDto(
                z.Id,
                z.Slug,
                z.City,
                z.SortOrder,
                [.. z.Translations.OrderBy(t => Locales.All.ToList().IndexOf(t.Locale)).Select(t => new ZoneTranslationDto(t.Locale, t.Name, t.Description))],
                counts.GetValueOrDefault(z.Id))),
        ];
    }
}

public sealed record SaveZoneCommand(Guid? Id, string? Slug, string City, int SortOrder, IReadOnlyList<ZoneTranslationDto> Translations) : ICommand<AdminZoneDto>;

public sealed class SaveZoneCommandValidator : AbstractValidator<SaveZoneCommand>
{
    public SaveZoneCommandValidator()
    {
        RuleFor(x => x.Slug).Slug().When(x => !string.IsNullOrEmpty(x.Slug));
        RuleFor(x => x.City).NotEmpty().MaximumLength(80);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Translations).TranslationsWithSpanish(t => t.Locale);
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).SupportedLocale();
            t.RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
            t.RuleFor(x => x.Description).MaximumLength(4000);
        });
    }
}

public sealed class SaveZoneCommandHandler(IAppDbContext db) : IRequestHandler<SaveZoneCommand, AdminZoneDto>
{
    public async Task<AdminZoneDto> Handle(SaveZoneCommand request, CancellationToken cancellationToken)
    {
        var spanishName = request.Translations.First(t => t.Locale == Locales.Default).Name;
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? SlugGenerator.Slugify(spanishName) : request.Slug;

        if (await db.Zones.AnyAsync(z => z.Slug == slug && z.Id != request.Id, cancellationToken))
        {
            throw new ConflictException(MessageKeys.ZoneSlugExists);
        }

        Zone zone;
        if (request.Id is { } id)
        {
            zone = await db.Zones.Include(z => z.Translations).FirstOrDefaultAsync(z => z.Id == id, cancellationToken) ?? throw new NotFoundException();
            zone.Update(slug, request.City, request.SortOrder);
        }
        else
        {
            zone = Zone.Create(slug, request.City, request.SortOrder);
            db.Zones.Add(zone);
        }

        foreach (var removed in zone.Translations.Where(t => request.Translations.All(r => r.Locale != t.Locale)).Select(t => t.Locale).ToList())
        {
            zone.RemoveTranslation(removed);
        }

        foreach (var translation in request.Translations)
        {
            zone.SetTranslation(translation.Locale, translation.Name, translation.Description);
        }

        var count = request.Id is null ? 0 : await db.Properties.CountAsync(p => p.ZoneId == zone.Id, cancellationToken);
        return new AdminZoneDto(zone.Id, zone.Slug, zone.City, zone.SortOrder,
            [.. zone.Translations.Select(t => new ZoneTranslationDto(t.Locale, t.Name, t.Description))], count);
    }
}

public sealed record DeleteZoneCommand(Guid Id) : ICommand;

public sealed class DeleteZoneCommandHandler(IAppDbContext db) : IRequestHandler<DeleteZoneCommand>
{
    public async Task Handle(DeleteZoneCommand request, CancellationToken cancellationToken)
    {
        var zone = await db.Zones.FirstOrDefaultAsync(z => z.Id == request.Id, cancellationToken) ?? throw new NotFoundException();
        if (await db.Properties.AnyAsync(p => p.ZoneId == zone.Id, cancellationToken))
        {
            throw new ConflictException(MessageKeys.ZoneInUse);
        }

        db.Zones.Remove(zone);
    }
}
