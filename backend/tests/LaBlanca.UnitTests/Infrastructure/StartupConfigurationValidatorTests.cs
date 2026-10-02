using LaBlanca.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace LaBlanca.UnitTests.Infrastructure;

public class StartupConfigurationValidatorTests
{
    private const string StrongKey = "0123456789abcdef0123456789abcdef0123456789";

    private static IConfiguration Config(params (string Key, string? Value)[] values)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=db.example.com;Database=lablanca;Username=app;Password=x;SSL Mode=VerifyFull",
            ["Jwt:Issuer"] = "lablanca-api",
            ["Jwt:Audience"] = "lablanca-site",
            ["Jwt:SigningKey"] = StrongKey,
            ["Site:RevalidateSecret"] = "a-long-revalidation-secret-value-123",
        };
        foreach (var (key, value) in values)
        {
            defaults[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(defaults).Build();
    }

    [Fact]
    public void Valid_production_configuration_has_no_errors()
    {
        StartupConfigurationValidator.Validate(Config(), "Production").Should().BeEmpty();
    }

    [Theory]
    [InlineData("Host=db;Database=lablanca;Username=app;Password=x")]
    [InlineData("Host=db;Database=lablanca;Username=app;Password=x;SSL Mode=Disable")]
    [InlineData("Host=db;Database=lablanca;Username=app;Password=x;SSL Mode=Prefer")]
    public void Production_requires_ssl(string connectionString)
    {
        var errors = StartupConfigurationValidator.Validate(Config(("ConnectionStrings:Default", connectionString)), "Production");

        errors.Should().ContainSingle(e => e.Contains("SSL Mode"));
    }

    [Theory]
    [InlineData("Require")]
    [InlineData("VerifyCA")]
    [InlineData("VerifyFull")]
    public void Production_accepts_secure_ssl_modes(string mode)
    {
        var config = Config(("ConnectionStrings:Default", $"Host=db;Database=lablanca;Username=app;Password=x;SSL Mode={mode}"));

        StartupConfigurationValidator.Validate(config, "Production").Should().BeEmpty();
    }

    [Fact]
    public void Development_accepts_local_database_without_ssl()
    {
        var config = Config(
            ("ConnectionStrings:Default", "Host=localhost;Database=lablanca;Username=postgres;Password=postgres"),
            ("Site:RevalidateSecret", null));

        StartupConfigurationValidator.Validate(config, "Development").Should().BeEmpty();
    }

    [Fact]
    public void Missing_connection_string_is_reported()
    {
        var errors = StartupConfigurationValidator.Validate(Config(("ConnectionStrings:Default", null)), "Development");

        errors.Should().ContainSingle(e => e.Contains("ConnectionStrings:Default"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short-key")]
    public void Jwt_signing_key_must_have_at_least_32_bytes(string? key)
    {
        var errors = StartupConfigurationValidator.Validate(Config(("Jwt:SigningKey", key)), "Development");

        errors.Should().ContainSingle(e => e.Contains("Jwt:SigningKey"));
    }

    [Fact]
    public void Production_requires_revalidation_secret()
    {
        var errors = StartupConfigurationValidator.Validate(Config(("Site:RevalidateSecret", null)), "Production");

        errors.Should().ContainSingle(e => e.Contains("Site:RevalidateSecret"));
    }

    [Fact]
    public void Production_never_migrates_on_startup()
    {
        var errors = StartupConfigurationValidator.Validate(Config(("Database:MigrateOnStartup", "true")), "Production");

        errors.Should().ContainSingle(e => e.Contains("Database:MigrateOnStartup"));
    }
}
