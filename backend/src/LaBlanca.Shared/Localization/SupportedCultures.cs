using System.Globalization;

namespace LaBlanca.Shared.Localization;

public static class SupportedCultures
{
    public const string Default = "pt";

    public static readonly string[] Names = ["pt", "es", "en", "gn"];

    public static CultureInfo[] All => [.. Names.Select(CultureInfo.GetCultureInfo)];
}
