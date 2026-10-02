using System.Collections;
using System.Globalization;
using LaBlanca.Shared.Localization;

namespace LaBlanca.UnitTests.Shared;

public class AppMessagesTests
{
    [Fact]
    public void Guarani_culture_can_be_created()
    {
        var act = () => CultureInfo.GetCultureInfo("gn");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    [InlineData("gn")]
    public void Culture_resources_have_exactly_the_neutral_keys(string culture)
    {
        var neutral = Keys(CultureInfo.InvariantCulture);
        var translated = Keys(CultureInfo.GetCultureInfo(culture));

        translated.Should().BeEquivalentTo(neutral);
    }

    [Theory]
    [InlineData("pt", "Um ou mais campos são inválidos.")]
    [InlineData("es-PY", "Uno o más campos no son válidos.")]
    [InlineData("en-US", "One or more fields are invalid.")]
    [InlineData("gn", "Uno o más campos no son válidos.")]
    [InlineData("fr", "Um ou mais campos são inválidos.")]
    public void Get_returns_text_for_current_ui_culture(string culture, string expected)
    {
        using var _ = new UiCultureScope(culture);

        AppMessages.Get(MessageKeys.Validation).Should().Be(expected);
    }

    [Fact]
    public void Get_returns_key_when_message_does_not_exist()
    {
        AppMessages.Get("Missing.Key").Should().Be("Missing.Key");
    }

    [Fact]
    public void Get_formats_arguments()
    {
        using var _ = new UiCultureScope("pt");

        AppMessages.Format("{0} de {1}", 1, 2).Should().Be("1 de 2");
    }

    private static HashSet<string> Keys(CultureInfo culture)
    {
        var set = AppMessages.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        set.Should().NotBeNull($"resources for '{culture.Name}' must exist");
        return set!.Cast<DictionaryEntry>().Select(e => (string)e.Key).ToHashSet();
    }
}

internal sealed class UiCultureScope : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    public UiCultureScope(string culture) => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

    public void Dispose() => CultureInfo.CurrentUICulture = _previous;
}
