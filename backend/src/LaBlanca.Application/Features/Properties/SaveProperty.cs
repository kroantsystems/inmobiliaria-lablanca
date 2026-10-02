using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Revalidation;
using LaBlanca.Application.Common;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Properties;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Properties;

/// <summary>Herda os campos do input para os erros de validação saírem com nomes planos (<c>price</c>, não <c>property.price</c>).</summary>
public sealed record SavePropertyCommand : PropertyInput, ICommand<Guid>
{
    public SavePropertyCommand(Guid? id, PropertyInput input)
        : base(input)
    {
        Id = id;
    }

    public Guid? Id { get; }
}

public sealed class PropertyInputValidator : AbstractValidator<PropertyInput>
{
    private static readonly string[] VideoHosts = ["youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be", "vimeo.com", "www.vimeo.com", "player.vimeo.com"];

    public PropertyInputValidator()
    {
        RuleFor(x => x.Operation).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Currency).IsInEnum();
        RuleFor(x => x.Price).GreaterThan(0).LessThan(1_000_000_000_000m);
        RuleFor(x => x.ZoneId).NotEmpty();
        RuleFor(x => x.City).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.Latitude).Latitude();
        RuleFor(x => x.Longitude).Longitude();
        RuleFor(x => x.Longitude).NotNull().When(x => x.Latitude is not null);
        RuleFor(x => x.Latitude).NotNull().When(x => x.Longitude is not null);
        RuleFor(x => x.Bedrooms).InclusiveBetween(0, 100);
        RuleFor(x => x.Bathrooms).InclusiveBetween(0, 100);
        RuleFor(x => x.ParkingSpaces).InclusiveBetween(0, 100);
        RuleFor(x => x.BuiltAreaM2).GreaterThan(0).When(x => x.BuiltAreaM2 is not null);
        RuleFor(x => x.LotAreaM2).GreaterThan(0).When(x => x.LotAreaM2 is not null);
        RuleFor(x => x.Features).Must(f => f is null || f.Count <= 40);
        RuleForEach(x => x.Features).NotEmpty().MaximumLength(60);
        RuleFor(x => x.VideoUrl).MaximumLength(500)
            .Must(IsSupportedVideo).WithMessage(_ => AppMessages.Get(MessageKeys.InvalidVideoUrl))
            .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl));
        RuleFor(x => x.Translations).TranslationsWithSpanish(t => t.Locale);
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).SupportedLocale();
            t.RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
            t.RuleFor(x => x.Description).MaximumLength(8000);
            t.RuleFor(x => x.SeoTitle).MaximumLength(70);
            t.RuleFor(x => x.SeoDescription).MaximumLength(170);
        });
    }

    public static bool IsSupportedVideo(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && (VideoHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase) || uri.AbsolutePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));
}

public sealed class SavePropertyCommandValidator : AbstractValidator<SavePropertyCommand>
{
    public SavePropertyCommandValidator() => Include(new PropertyInputValidator());
}

public sealed class SavePropertyCommandHandler(IAppDbContext db, IRevalidationNotifier revalidation) : IRequestHandler<SavePropertyCommand, Guid>
{
    private const string FallbackSlug = "propiedad";

    public async Task<Guid> Handle(SavePropertyCommand input, CancellationToken cancellationToken)
    {
        if (!await db.Zones.AnyAsync(z => z.Id == input.ZoneId, cancellationToken))
        {
            throw new BusinessRuleException(MessageKeys.PropertyZoneNotFound);
        }

        if (input.OwnerId is { } ownerId && !await db.Owners.AnyAsync(o => o.Id == ownerId, cancellationToken))
        {
            throw new BusinessRuleException(MessageKeys.PropertyOwnerNotFound);
        }

        Property property;
        if (input.Id is { } id)
        {
            property = await db.Properties.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                ?? throw new NotFoundException();
        }
        else
        {
            property = Property.Create(input.Operation, input.Type, input.Price, input.Currency, input.ZoneId);
            db.Properties.Add(property);
        }

        property.UpdateDetails(new PropertyDetails(
            input.Operation, input.Type, input.Price, input.Currency, input.ZoneId, input.City, input.Address, input.Latitude, input.Longitude,
            input.Bedrooms, input.Bathrooms, input.BuiltAreaM2, input.LotAreaM2, input.ParkingSpaces, input.Features ?? [], input.VideoUrl, input.OwnerId));

        foreach (var locale in property.Translations.Select(t => t.Locale).Where(l => input.Translations.All(t => t.Locale != l)).ToList())
        {
            property.RemoveTranslation(locale);
        }

        foreach (var t in input.Translations)
        {
            // Slug só muda quando o título muda, para não quebrar links já indexados.
            var existing = property.TranslationFor(t.Locale);
            var slug = existing is not null && existing.Title == t.Title.Trim()
                ? existing.Slug
                : await UniqueSlugAsync(property.Id, t.Locale, t.Title, cancellationToken);
            property.SetTranslation(t.Locale, t.Title, slug, t.Description, t.SeoTitle, t.SeoDescription);
        }

        revalidation.Request(CacheTags.ForProperty(property.Id));
        return property.Id;
    }

    private async Task<string> UniqueSlugAsync(Guid propertyId, string locale, string title, CancellationToken cancellationToken)
    {
        var slug = SlugGenerator.Slugify(title);
        if (slug.Length == 0)
        {
            slug = FallbackSlug;
        }

        var prefix = slug + "-";
        var taken = await db.PropertyTranslations
            .Where(t => t.Locale == locale && t.PropertyId != propertyId && (t.Slug == slug || t.Slug.StartsWith(prefix)))
            .Select(t => t.Slug)
            .ToListAsync(cancellationToken);
        return SlugGenerator.MakeUnique(slug, taken);
    }
}
