using System.Globalization;
using System.Resources;

namespace LaBlanca.Shared.Localization;

/// <summary>Textos localizados conforme <see cref="CultureInfo.CurrentUICulture"/>, definida por requisição.</summary>
public static class AppMessages
{
    public static ResourceManager ResourceManager { get; } =
        new("LaBlanca.Shared.Resources.Messages", typeof(AppMessages).Assembly);

    public static string Get(string key) => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Get(string key, params object?[] args) => Format(Get(key), args);

    public static string Format(string template, params object?[] args) =>
        args.Length == 0 ? template : string.Format(CultureInfo.CurrentCulture, template, args);
}
