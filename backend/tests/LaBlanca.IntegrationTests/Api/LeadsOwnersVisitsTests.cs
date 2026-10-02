using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaBlanca.Application.Authorization;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class LeadsOwnersVisitsTests(ApiFactory factory) : IAsyncLifetime
{
    private static readonly Guid Zone = Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05");

    private HttpClient Admin => factory.CreateClientWithToken(Permissions.All.ToArray());

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static object PublicLead(string source = "Contact", string phone = "+595981123456", string? email = null, bool consent = true, string? website = null, Guid? propertyId = null) => new
    {
        source,
        name = "Rodrigo Silva",
        phone,
        email,
        interest = "BuyHouse",
        propertyId,
        message = "Quiero visitar.",
        locale = "es",
        consent,
        website,
    };

    private async Task<int> LeadCountAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Leads.IgnoreQueryFilters().CountAsync();
    }

    private async Task<Guid> CreatePropertyAsync(Guid? ownerId = null, string title = "Casa para visitar")
    {
        var response = await Admin.PostAsJsonAsync("/api/admin/properties", new
        {
            operation = "Sale",
            type = "House",
            price = 100_000m,
            currency = "USD",
            zoneId = Zone,
            city = "Hernandarias",
            ownerId,
            translations = new[] { new { locale = "es", title } },
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Public_lead_is_accepted_without_exposing_data()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/public/leads", PublicLead());

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await LeadCountAsync()).Should().Be(1);

        var list = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/leads");
        var lead = list.GetProperty("items")[0];
        lead.GetProperty("status").GetString().Should().Be("New");
        lead.GetProperty("source").GetString().Should().Be("Contact");
        lead.GetProperty("locale").GetString().Should().Be("es");
        lead.GetProperty("consentAt").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Honeypot_filled_returns_202_without_saving()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/public/leads", PublicLead(website: "http://spam.example"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await LeadCountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("Newsletter", "+595981123456", null, true, "email")]
    [InlineData("Contact", "+595981123456", null, false, "consent")]
    [InlineData("Contact", "123-45", null, true, "phone")]
    [InlineData("Manual", "+595981123456", null, true, "source")]
    public async Task Invalid_public_lead_is_rejected(string source, string phone, string? email, bool consent, string field)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/leads") { Content = JsonContent.Create(PublicLead(source, phone, email, consent)) };
        request.Headers.Add("Accept-Language", "es");

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Visit_request_is_linked_to_published_property()
    {
        var propertyId = await CreatePropertyAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var property = await db.Properties.Include(p => p.Media).SingleAsync(p => p.Id == propertyId);
            property.AddMedia(LaBlanca.Domain.Media.MediaFile.Create(propertyId, "f.jpg", $"t/{Guid.NewGuid()}.jpg", "image/jpeg", LaBlanca.Domain.Media.MediaKind.Image, 1, Guid.NewGuid(), DateTimeOffset.UtcNow, true));
            property.ChangeStatus(LaBlanca.Domain.Properties.PropertyStatus.Available, DateTimeOffset.UtcNow);
            property.Publish(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        (await factory.CreateClient().PostAsJsonAsync("/api/public/leads", PublicLead("VisitRequest", propertyId: propertyId))).StatusCode.Should().Be(HttpStatusCode.Accepted);

        var lead = (await Admin.GetFromJsonAsync<JsonElement>("/api/admin/leads")).GetProperty("items")[0];
        lead.GetProperty("propertyId").GetGuid().Should().Be(propertyId);
        lead.GetProperty("propertyTitle").GetString().Should().Be("Casa para visitar");
    }

    [Fact]
    public async Task Admin_creates_manual_lead_and_changes_status()
    {
        var created = await Admin.PostAsJsonAsync("/api/admin/leads", new { name = "María Fernández", phone = "+595973444555", email = "maria@example.com", interest = "RentApartment" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await Admin.PutAsJsonAsync($"/api/admin/leads/{id}/status", new { status = "Negotiating" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lead = (await Admin.GetFromJsonAsync<JsonElement>("/api/admin/leads?pageSize=500")).GetProperty("items")[0];
        lead.GetProperty("source").GetString().Should().Be("Manual");
        lead.GetProperty("status").GetString().Should().Be("Negotiating");
        lead.GetProperty("updatedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Leads_require_leads_permission()
    {
        (await factory.CreateClientWithToken(Permissions.PropertiesRead).GetAsync("/api/admin/leads")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_proposal_is_converted_into_owner()
    {
        await factory.CreateClient().PostAsJsonAsync("/api/public/leads", PublicLead("OwnerProposal", email: "dueno@example.com"));
        var leadId = (await Admin.GetFromJsonAsync<JsonElement>("/api/admin/leads")).GetProperty("items")[0].GetProperty("id").GetGuid();

        var converted = await Admin.PostAsync($"/api/admin/leads/{leadId}/convert-to-owner", null);

        converted.StatusCode.Should().Be(HttpStatusCode.Created);
        var owner = (await Admin.GetFromJsonAsync<JsonElement>("/api/admin/owners")).GetProperty("items")[0];
        owner.GetProperty("name").GetString().Should().Be("Rodrigo Silva");
        owner.GetProperty("email").GetString().Should().Be("dueno@example.com");
        (await Admin.GetFromJsonAsync<JsonElement>("/api/admin/leads")).GetProperty("items")[0].GetProperty("status").GetString().Should().Be("Won");
    }

    [Fact]
    public async Task Owner_lists_captured_properties_and_cannot_be_deleted_while_active()
    {
        var created = await Admin.PostAsJsonAsync("/api/admin/owners", new { name = "Don Hernán Gómez", phone = "+595981777888" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var ownerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var first = await CreatePropertyAsync(ownerId, "Residencia Aura Light");
        await CreatePropertyAsync(ownerId, "Sky Horizon Duplex");

        var owner = (await Admin.GetFromJsonAsync<JsonElement>("/api/admin/owners")).GetProperty("items")[0];
        owner.GetProperty("properties").EnumerateArray().Select(p => p.GetProperty("title").GetString())
            .Should().BeEquivalentTo("Residencia Aura Light", "Sky Horizon Duplex");

        (await Admin.DeleteAsync($"/api/admin/owners/{ownerId}")).StatusCode.Should().Be(HttpStatusCode.Conflict);

        foreach (var property in owner.GetProperty("properties").EnumerateArray())
        {
            (await Admin.PostAsync($"/api/admin/properties/{property.GetProperty("id").GetGuid()}/archive", null)).EnsureSuccessStatusCode();
        }

        (await Admin.DeleteAsync($"/api/admin/owners/{ownerId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Admin.GetFromJsonAsync<JsonElement>($"/api/admin/properties/{first}")).GetProperty("ownerId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Owner_requires_name()
    {
        (await Admin.PostAsJsonAsync("/api/admin/owners", new { name = "", phone = "+595981777888" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Visit_scheduling_updates_lead_and_rejects_past_dates_and_conflicts()
    {
        var propertyId = await CreatePropertyAsync();
        var leadResponse = await Admin.PostAsJsonAsync("/api/admin/leads", new { name = "Carlos Mendoza", phone = "+595981000111", interest = "BuyHouse" });
        var leadId = (await leadResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var start = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(13);

        var past = await Admin.PostAsJsonAsync("/api/admin/visits", new { propertyId, leadId, startsAt = DateTimeOffset.UtcNow.AddHours(-1) });
        past.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var created = await Admin.PostAsJsonAsync("/api/admin/visits", new { propertyId, leadId, startsAt = start, notes = "Llevar llaves" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var visit = await created.Content.ReadFromJsonAsync<JsonElement>();
        visit.GetProperty("status").GetString().Should().Be("Scheduled");
        visit.GetProperty("clientName").GetString().Should().Be("Carlos Mendoza");
        visit.GetProperty("durationMinutes").GetInt32().Should().Be(60);

        var lead = (await Admin.GetFromJsonAsync<JsonElement>("/api/admin/leads")).GetProperty("items")[0];
        lead.GetProperty("status").GetString().Should().Be("VisitScheduled");

        var conflict = await Admin.PostAsJsonAsync("/api/admin/visits", new { propertyId, clientName = "Otro", startsAt = start.AddMinutes(30) });
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var afterwards = await Admin.PostAsJsonAsync("/api/admin/visits", new { propertyId, clientName = "Otro", startsAt = start.AddMinutes(60) });
        afterwards.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Calendar_range_upcoming_and_cancellation()
    {
        var propertyId = await CreatePropertyAsync();
        var baseDate = DateTimeOffset.UtcNow.AddDays(1).Date;
        for (var i = 0; i < 12; i++)
        {
            (await Admin.PostAsJsonAsync("/api/admin/visits", new { propertyId, clientName = $"Cliente {i}", startsAt = baseDate.AddDays(i).AddHours(14) }))
                .StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var from = baseDate.ToString("O");
        var to = baseDate.AddDays(5).ToString("O");
        var range = await Admin.GetFromJsonAsync<JsonElement>($"/api/admin/visits?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");
        range.GetArrayLength().Should().Be(5);
        range[0].GetProperty("propertyTitle").GetString().Should().Be("Casa para visitar");

        var tooLarge = await Admin.GetAsync($"/api/admin/visits?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(baseDate.AddDays(63).ToString("O"))}");
        tooLarge.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var upcoming = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/visits/upcoming");
        upcoming.GetArrayLength().Should().Be(10);
        var firstId = upcoming[0].GetProperty("id").GetGuid();

        (await Admin.PutAsJsonAsync($"/api/admin/visits/{firstId}/status", new { status = "Cancelled" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterCancel = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/visits/upcoming");
        afterCancel.EnumerateArray().Select(v => v.GetProperty("id").GetGuid()).Should().NotContain(firstId);
    }
}
