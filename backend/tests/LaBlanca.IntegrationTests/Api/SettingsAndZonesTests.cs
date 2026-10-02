using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaBlanca.Application.Authorization;
using LaBlanca.Domain.Properties;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class SettingsAndZonesTests(ApiFactory factory) : IAsyncLifetime
{
    private static object ValidSettings(string whatsapp = "+595981123456", decimal pyg = 7450m) => new
    {
        companyName = "Inmobiliaria La Blanca",
        phone = "+595 61 500 000",
        whatsappNumber = whatsapp,
        email = "contacto@lablanca.com.py",
        address = "Av. San Blas, Ciudad del Este",
        officeLatitude = -25.5097,
        officeLongitude = -54.6111,
        openingHours = "Lun a Vie 8:00–18:00",
        facebookUrl = "https://facebook.com/lablanca",
        instagramUrl = "https://instagram.com/lablanca",
        tiktokUrl = (string?)null,
        youtubeUrl = (string?)null,
        pygPerUsd = pyg,
        brlPerUsd = 5.4m,
        simulatorAnnualRate = 8.5m,
        monthlySalesGoal = 12,
    };

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();

    public async Task DisposeAsync()
    {
        // Restaura o seed alterado pelos testes (tabelas com seed não são limpas pelo Respawn).
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE lablanca."SiteSettings" SET "Phone" = NULL, "WhatsappNumber" = NULL, "Email" = NULL, "Address" = NULL,
              "OfficeLatitude" = NULL, "OfficeLongitude" = NULL, "OpeningHours" = NULL, "FacebookUrl" = NULL, "InstagramUrl" = NULL,
              "PygPerUsd" = 7500, "BrlPerUsd" = 5, "SimulatorAnnualRate" = 8, "MonthlySalesGoal" = 10, "UpdatedAt" = NULL;
            DELETE FROM lablanca."Zones" WHERE "Slug" LIKE 'test-%';
            """);
    }

    [Fact]
    public async Task Admin_updates_settings_and_public_view_hides_sales_goal()
    {
        var admin = factory.CreateClientWithToken(Permissions.SettingsRead, Permissions.SettingsWrite);

        var update = await admin.PutAsJsonAsync("/api/admin/settings", ValidSettings());
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var adminView = await admin.GetFromJsonAsync<JsonElement>("/api/admin/settings");
        adminView.GetProperty("monthlySalesGoal").GetInt32().Should().Be(12);
        adminView.GetProperty("pygPerUsd").GetDecimal().Should().Be(7450m);

        var publicResponse = await factory.CreateClient().GetAsync("/api/public/settings?locale=es");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicBody = await publicResponse.Content.ReadAsStringAsync();
        var publicView = JsonDocument.Parse(publicBody).RootElement;
        publicView.GetProperty("whatsappNumber").GetString().Should().Be("+595981123456");
        publicView.GetProperty("pygPerUsd").GetDecimal().Should().Be(7450m);
        publicView.GetProperty("ratesUpdatedAt").GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        publicBody.Should().NotContainEquivalentOf("monthlySalesGoal");
        publicView.GetProperty("zones").GetArrayLength().Should().Be(5);
    }

    [Theory]
    [InlineData("0981123456")]
    [InlineData("+595 981 123")]
    [InlineData("+59598112345678901")]
    public async Task Whatsapp_must_be_international_format(string whatsapp)
    {
        var admin = factory.CreateClientWithToken(Permissions.SettingsWrite);

        var response = await admin.PutAsJsonAsync("/api/admin/settings", ValidSettings(whatsapp: whatsapp));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("whatsappNumber", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Exchange_rate_must_be_positive(decimal pyg)
    {
        var admin = factory.CreateClientWithToken(Permissions.SettingsWrite);

        var response = await admin.PutAsJsonAsync("/api/admin/settings", ValidSettings(pyg: pyg));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Settings_require_permissions()
    {
        (await factory.CreateClient().GetAsync("/api/admin/settings")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await factory.CreateClientWithToken(Permissions.LeadsRead).PutAsJsonAsync("/api/admin/settings", ValidSettings()))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_creates_zone_that_appears_in_public_list_with_translated_name()
    {
        var admin = factory.CreateClientWithToken(Permissions.SettingsWrite, Permissions.PropertiesRead);

        var created = await admin.PostAsJsonAsync("/api/admin/zones", new
        {
            slug = "test-presidente-franco",
            city = "Presidente Franco",
            sortOrder = 9,
            translations = new[]
            {
                new { locale = "es", name = "Presidente Franco", description = "Zona residencial a orillas del Paraná." },
                new { locale = "pt", name = "Presidente Franco", description = "Zona residencial às margens do Paraná." },
            },
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var zones = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/zones?locale=pt");
        var zone = zones.EnumerateArray().Single(z => z.GetProperty("slug").GetString() == "test-presidente-franco");
        zone.GetProperty("description").GetString().Should().Be("Zona residencial às margens do Paraná.");

        var english = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/zones/test-presidente-franco?locale=en");
        english.GetProperty("description").GetString().Should().Be("Zona residencial a orillas del Paraná.", "missing translation falls back to Spanish");
    }

    [Fact]
    public async Task Zone_requires_spanish_name_and_unique_slug()
    {
        var admin = factory.CreateClientWithToken(Permissions.SettingsWrite);

        var withoutSpanish = await admin.PostAsJsonAsync("/api/admin/zones", new
        {
            slug = "test-sem-es",
            city = "CDE",
            sortOrder = 1,
            translations = new[] { new { locale = "pt", name = "Só português", description = (string?)null } },
        });
        withoutSpanish.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var duplicated = await admin.PostAsJsonAsync("/api/admin/zones", new
        {
            slug = "hernandarias",
            city = "Hernandarias",
            sortOrder = 1,
            translations = new[] { new { locale = "es", name = "Hernandarias 2", description = (string?)null } },
        });
        duplicated.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Zone_in_use_cannot_be_deleted()
    {
        var admin = factory.CreateClientWithToken(Permissions.SettingsWrite);
        var created = await admin.PostAsJsonAsync("/api/admin/zones", new
        {
            slug = "test-em-uso",
            city = "CDE",
            sortOrder = 1,
            translations = new[] { new { locale = "es", name = "En uso", description = (string?)null } },
        });
        var zoneId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Properties.Add(Property.Create(PropertyOperation.Sale, PropertyType.House, 100_000m, Currency.USD, zoneId));
            await db.SaveChangesAsync();
        }

        (await admin.DeleteAsync($"/api/admin/zones/{zoneId}")).StatusCode.Should().Be(HttpStatusCode.Conflict);

        await factory.ResetDatabaseAsync();
        (await admin.DeleteAsync($"/api/admin/zones/{zoneId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
