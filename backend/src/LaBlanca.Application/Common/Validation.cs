using FluentValidation;
using LaBlanca.Domain.Common;
using LaBlanca.Shared.Localization;

namespace LaBlanca.Application.Common;

public static class ContentLocale
{
    /// <summary>Idioma de conteúdo pedido pelo site; espanhol quando ausente ou não suportado.</summary>
    public static string Resolve(string? locale) => Locales.IsSupported(locale) ? locale! : Locales.Default;
}

public static class CommonRules
{
    public const string InternationalPhonePattern = @"^\+[1-9]\d{7,14}$";

    public static IRuleBuilderOptions<T, string?> SupportedLocale<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(Locales.IsSupported).WithMessage(_ => AppMessages.Get(MessageKeys.UnsupportedLocale));

    public static IRuleBuilderOptions<T, string?> InternationalPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches(InternationalPhonePattern).WithMessage(_ => AppMessages.Get(MessageKeys.InternationalPhone));

    public static IRuleBuilderOptions<T, string?> AbsoluteHttpUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            .WithMessage(_ => AppMessages.Get(MessageKeys.InvalidUrl));

    public static IRuleBuilderOptions<T, string?> Slug<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").MaximumLength(SlugGenerator.MaxLength)
            .WithMessage(_ => AppMessages.Get(MessageKeys.InvalidSlug));

    public static IRuleBuilderOptions<T, double?> Latitude<T>(this IRuleBuilder<T, double?> rule) => rule.InclusiveBetween(-90, 90);

    public static IRuleBuilderOptions<T, double?> Longitude<T>(this IRuleBuilder<T, double?> rule) => rule.InclusiveBetween(-180, 180);

    /// <summary>Lista de traduções com espanhol obrigatório e no máximo uma por idioma.</summary>
    public static IRuleBuilderOptions<T, IReadOnlyList<TTranslation>> TranslationsWithSpanish<T, TTranslation>(
        this IRuleBuilder<T, IReadOnlyList<TTranslation>> rule, Func<TTranslation, string> locale) =>
        rule.NotNull()
            .Must(list => list.Any(t => locale(t) == Locales.Default))
            .WithMessage(_ => AppMessages.Get(MessageKeys.SpanishTranslationRequired))
            .Must(list => list.Select(locale).Distinct().Count() == list.Count)
            .WithMessage(_ => AppMessages.Get(MessageKeys.DuplicatedLocale));
}
