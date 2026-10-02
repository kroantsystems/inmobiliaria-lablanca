namespace LaBlanca.Domain.Common;

/// <summary>Idiomas de conteúdo. Espanhol é obrigatório e serve de fallback.</summary>
public static class Locales
{
    public const string Default = "es";

    public static readonly IReadOnlyList<string> All = ["es", "pt", "en", "gn"];

    public static bool IsSupported(string? locale) => locale is not null && All.Contains(locale);
}
