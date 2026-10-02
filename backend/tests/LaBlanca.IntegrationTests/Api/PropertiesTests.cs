using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaBlanca.Application.Abstractions.Revalidation;
using LaBlanca.Application.Authorization;
using LaBlanca.Domain.Media;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.Infrastructure.Persistence.Seed;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class PropertiesTests(ApiFactory factory) : IAsyncLifetime
{
    private static readonly Guid HernandariasZone = Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05");
    private static readonly Guid CountryClubZone = Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e01");

    private HttpClient Admin => factory.CreateClientWithToken(Permissions.PropertiesRead, Permissions.PropertiesWrite);

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static object NewProperty(
        string title = "Residencia Aura Light – Área 1",
        decimal price = 350_000m,
        string currency = "USD",
        string operation = "Sale",
        string type = "House",
        Guid? zoneId = null,
        int bedrooms = 4,
        double? latitude = -25.51,
        string? videoUrl = "https://www.youtube.com/watch?v=abc123",
        object[]? translations = null) => new
        {
            operation,
            type,
            price,
            currency,
            zoneId = zoneId ?? HernandariasZone,
            city = "Hernandarias",
            address = "Calle 1",
            latitude,
            longitude = -54.64,
            bedrooms,
            bathrooms = 5,
            builtAreaM2 = 620m,
            lotAreaM2 = 900m,
            parkingSpaces = 2,
            features = new[] { "Piscina", "Quincho" },
            videoUrl,
            ownerId = (Guid?)null,
            translations = translations ?? [new { locale = "es", title, description = "Casa amplia.", seoTitle = (string?)null, seoDescription = (string?)null }],
        };

    private async Task<JsonElement> CreateAsync(object body)
    {
        var response = await Admin.PostAsJsonAsync("/api/admin/properties", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task AddPublicImageAsync(Guid propertyId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var property = await db.Properties.Include(p => p.Media).SingleAsync(p => p.Id == propertyId);
        property.AddMedia(MediaFile.Create(propertyId, "fachada.jpg", $"test/{Guid.NewGuid()}.jpg", "image/jpeg", MediaKind.Image, 1000, Guid.NewGuid(), DateTimeOffset.UtcNow, isPublic: true));
        await db.SaveChangesAsync();
    }

    private async Task<Guid> CreatePublishedAsync(object body)
    {
        var id = (await CreateAsync(body)).GetProperty("id").GetGuid();
        await AddPublicImageAsync(id);
        (await Admin.PutAsJsonAsync($"/api/admin/properties/{id}/status", new { status = "Available" })).EnsureSuccessStatusCode();
        (await Admin.PostAsync($"/api/admin/properties/{id}/publish", null)).EnsureSuccessStatusCode();
        return id;
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Valid_property_is_created_as_unpublished_draft_with_slug()
    {
        var created = await CreateAsync(NewProperty());

        created.GetProperty("status").GetString().Should().Be("Draft");
        created.GetProperty("isPublished").GetBoolean().Should().BeFalse();
        created.GetProperty("translations")[0].GetProperty("slug").GetString().Should().Be("residencia-aura-light-area-1");
    }

    [Fact]
    public async Task Repeated_title_receives_numeric_suffix()
    {
        await CreateAsync(NewProperty());
        var second = await CreateAsync(NewProperty());

        second.GetProperty("translations")[0].GetProperty("slug").GetString().Should().Be("residencia-aura-light-area-1-2");
    }

    [Fact]
    public async Task Invalid_property_data_is_rejected_by_field()
    {
        var noSpanish = await Admin.PostAsJsonAsync("/api/admin/properties", NewProperty(translations: [new { locale = "pt", title = "Casa", description = "", seoTitle = (string?)null, seoDescription = (string?)null }]));
        noSpanish.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadAsync(noSpanish)).GetProperty("errors").TryGetProperty("translations", out _).Should().BeTrue();

        var zeroPrice = await Admin.PostAsJsonAsync("/api/admin/properties", NewProperty(price: 0));
        (await ReadAsync(zeroPrice)).GetProperty("errors").TryGetProperty("price", out _).Should().BeTrue();

        var badLatitude = await Admin.PostAsJsonAsync("/api/admin/properties", NewProperty(latitude: 120));
        (await ReadAsync(badLatitude)).GetProperty("errors").TryGetProperty("latitude", out _).Should().BeTrue();

        var badVideo = await Admin.PostAsJsonAsync("/api/admin/properties", NewProperty(videoUrl: "http://example.com/video.avi"));
        (await ReadAsync(badVideo)).GetProperty("errors").TryGetProperty("videoUrl", out _).Should().BeTrue();

        var unknownZone = await Admin.PostAsJsonAsync("/api/admin/properties", NewProperty(zoneId: Guid.NewGuid()));
        unknownZone.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Publication_requires_available_status_and_public_image()
    {
        var id = (await CreateAsync(NewProperty())).GetProperty("id").GetGuid();

        (await Admin.PostAsync($"/api/admin/properties/{id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await Admin.PutAsJsonAsync($"/api/admin/properties/{id}/status", new { status = "Available" })).EnsureSuccessStatusCode();
        var withoutImage = await Admin.PostAsync($"/api/admin/properties/{id}/publish", null);
        withoutImage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadAsync(withoutImage)).GetProperty("detail").GetString().Should().Contain("imagem pública");

        await AddPublicImageAsync(id);
        var published = await Admin.PostAsync($"/api/admin/properties/{id}/publish", null);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(published)).GetProperty("publishedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Public_search_lists_only_published_available_properties_with_filters()
    {
        var house = await CreatePublishedAsync(NewProperty(title: "Casa Hernandarias", bedrooms: 4));
        await CreatePublishedAsync(NewProperty(title: "Departamento Country", type: "Apartment", zoneId: CountryClubZone, bedrooms: 2));
        await CreatePublishedAsync(NewProperty(title: "Casa alquiler", operation: "Rent", price: 2_000m, bedrooms: 3));
        await CreateAsync(NewProperty(title: "Casa borrador"));

        var all = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties?locale=es");
        all.GetProperty("total").GetInt32().Should().Be(3);

        var filtered = await factory.CreateClient().GetFromJsonAsync<JsonElement>(
            "/api/public/properties?locale=es&operation=Sale&type=House&zone=hernandarias&minBedrooms=3");
        filtered.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).Should().Equal(house);

        var oversized = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties?pageSize=100");
        oversized.GetProperty("pageSize").GetInt32().Should().Be(24);
    }

    [Fact]
    public async Task Price_filter_and_sort_convert_currencies_to_usd()
    {
        var guaranies = await CreatePublishedAsync(NewProperty(title: "Casa en guaraníes", price: 750_000_000m, currency: "PYG"));
        var dollars = await CreatePublishedAsync(NewProperty(title: "Casa en dólares", price: 200_000m));

        var cheap = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties?maxPrice=150000");
        cheap.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).Should().Equal(guaranies);

        var byPrice = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties?sort=price_desc");
        byPrice.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).Should().Equal(dollars, guaranies);
    }

    [Fact]
    public async Task Public_detail_by_slug_falls_back_to_spanish_and_exposes_slugs_by_locale()
    {
        var id = await CreatePublishedAsync(NewProperty(translations:
        [
            new { locale = "es", title = "Casa con piscina", description = "Descripción en español.", seoTitle = (string?)null, seoDescription = (string?)null },
            new { locale = "en", title = "House with pool", description = "English description.", seoTitle = (string?)"Pool house in Hernandarias", seoDescription = (string?)null },
        ]));

        var spanish = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties/es/casa-con-piscina");
        spanish.GetProperty("id").GetGuid().Should().Be(id);
        spanish.GetProperty("slugs").GetProperty("en").GetString().Should().Be("house-with-pool");
        spanish.GetProperty("slugs").GetProperty("pt").GetString().Should().Be("casa-con-piscina");

        var portuguese = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties/pt/casa-con-piscina");
        portuguese.GetProperty("locale").GetString().Should().Be("es");
        portuguese.GetProperty("description").GetString().Should().Be("Descripción en español.");

        var english = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties/en/house-with-pool");
        english.GetProperty("title").GetString().Should().Be("House with pool");
        english.GetProperty("seoTitle").GetString().Should().Be("Pool house in Hernandarias");
    }

    [Fact]
    public async Task Sold_or_archived_property_leaves_the_public_site()
    {
        var id = await CreatePublishedAsync(NewProperty(title: "Casa vendida"));

        (await Admin.PutAsJsonAsync($"/api/admin/properties/{id}/status", new { status = "Sold" })).EnsureSuccessStatusCode();

        (await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties")).GetProperty("total").GetInt32().Should().Be(0);
        (await factory.CreateClient().GetAsync("/api/public/properties/es/casa-vendida")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var detail = await Admin.GetFromJsonAsync<JsonElement>($"/api/admin/properties/{id}");
        detail.GetProperty("soldAt").ValueKind.Should().NotBe(JsonValueKind.Null);

        (await Admin.PostAsync($"/api/admin/properties/{id}/archive", null)).EnsureSuccessStatusCode();
        (await Admin.GetFromJsonAsync<JsonElement>($"/api/admin/properties/{id}")).GetProperty("isPublished").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Featured_requires_publication_and_falls_back_to_most_recent()
    {
        var draft = (await CreateAsync(NewProperty(title: "Casa borrador"))).GetProperty("id").GetGuid();
        (await Admin.PutAsJsonAsync($"/api/admin/properties/{draft}/featured", new { featured = true })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var older = await CreatePublishedAsync(NewProperty(title: "Casa antigua"));
        var newer = await CreatePublishedAsync(NewProperty(title: "Casa nueva"));

        var withoutFeatured = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties/featured");
        withoutFeatured[0].GetProperty("id").GetGuid().Should().Be(newer);

        (await Admin.PutAsJsonAsync($"/api/admin/properties/{older}/featured", new { featured = true })).EnsureSuccessStatusCode();
        var featured = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties/featured");
        featured.EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).Should().Equal(older);
    }

    [Fact]
    public async Task Admin_list_returns_every_status_in_large_pages()
    {
        await CreatePublishedAsync(NewProperty(title: "Publicada"));
        await CreateAsync(NewProperty(title: "Borrador"));

        var list = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/properties?pageSize=500");

        list.GetProperty("total").GetInt32().Should().Be(2);
        list.GetProperty("pageSize").GetInt32().Should().Be(500);
        list.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("status").GetString()).Should().BeEquivalentTo("Available", "Draft");
    }

    [Fact]
    public async Task Property_of_another_tenant_is_not_found()
    {
        var id = (await CreateAsync(NewProperty())).GetProperty("id").GetGuid();
        var otherTenant = factory.CreateClient();
        otherTenant.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Create([Permissions.PropertiesRead], tenantId: Guid.NewGuid()));

        (await otherTenant.GetAsync($"/api/admin/properties/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publishing_notifies_site_revalidation_after_commit()
    {
        var dispatcher = new CapturingDispatcher();
        await using var app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IRevalidationDispatcher>(dispatcher)));
        var admin = app.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Create([Permissions.PropertiesRead, Permissions.PropertiesWrite]));

        var created = await (await admin.PostAsJsonAsync("/api/admin/properties", NewProperty())).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        await AddPublicImageAsync(id);
        (await admin.PutAsJsonAsync($"/api/admin/properties/{id}/status", new { status = "Available" })).EnsureSuccessStatusCode();
        dispatcher.Tags.Clear();

        (await admin.PostAsync($"/api/admin/properties/{id}/publish", null)).EnsureSuccessStatusCode();

        dispatcher.Tags.Should().Contain(["properties", $"property:{id}", "sitemap"]);
    }

    [Fact]
    public async Task Failed_operation_does_not_notify_revalidation()
    {
        var dispatcher = new CapturingDispatcher();
        await using var app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IRevalidationDispatcher>(dispatcher)));
        var admin = app.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Create([Permissions.PropertiesWrite]));
        var id = (await CreateAsync(NewProperty())).GetProperty("id").GetGuid();
        dispatcher.Tags.Clear();

        (await admin.PostAsync($"/api/admin/properties/{id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        dispatcher.Tags.Should().BeEmpty();
    }

    private sealed class CapturingDispatcher : IRevalidationDispatcher
    {
        public ConcurrentBag<string> Tags { get; } = [];

        public void Dispatch(IReadOnlyCollection<string> tags)
        {
            foreach (var tag in tags)
            {
                Tags.Add(tag);
            }
        }
    }
}
