using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaBlanca.Application.Authorization;
using LaBlanca.Application.Features.Analytics;
using LaBlanca.Domain.Analytics;
using LaBlanca.Domain.Media;
using LaBlanca.Domain.Properties;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class AnalyticsTests(ApiFactory factory) : IAsyncLifetime
{
    private static readonly Guid Zone = Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05");
    private static readonly TimeZoneInfo Asuncion = TimeZoneInfo.FindSystemTimeZoneById("America/Asuncion");

    private HttpClient Admin => factory.CreateClientWithToken(Permissions.All.ToArray());

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static DateTimeOffset MonthStartUtc(int monthsAgo = 0)
    {
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Asuncion);
        var first = new DateTime(local.Year, local.Month, 1).AddMonths(-monthsAgo);
        return new DateTimeOffset(first, Asuncion.GetUtcOffset(first)).ToUniversalTime();
    }

    private static DateTimeOffset ThisMonth()
    {
        var candidate = DateTimeOffset.UtcNow.AddMinutes(-1);
        var start = MonthStartUtc().AddSeconds(1);
        return candidate < start ? start : candidate;
    }

    private async Task SeedAsync(Action<AppDbContext> seed)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }

    private static AnalyticsEvent Event(AnalyticsEventType type, DateTimeOffset at, Guid? session = null, Guid? propertyId = null) =>
        AnalyticsEvent.Record(type, propertyId, "/es", "es", session ?? Guid.NewGuid(), null, at);

    private static Property NewProperty(string title)
    {
        var property = Property.Create(PropertyOperation.Sale, PropertyType.House, 100_000m, Currency.USD, Zone);
        property.SetTranslation("es", title, title.ToLowerInvariant().Replace(' ', '-'), null, null, null);
        return property;
    }

    [Fact]
    public async Task Event_is_recorded_without_personal_data()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/analytics/events")
        {
            Content = JsonContent.Create(new { type = "PageView", path = "/es", locale = "es", sessionId = Guid.NewGuid(), referrer = "https://www.google.com/search?q=casas" }),
        };
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0) Chrome/130.0");

        (await factory.CreateClient().SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<AppDbContext>().AnalyticsEvents.IgnoreQueryFilters().SingleAsync();
        stored.ReferrerHost.Should().Be("www.google.com");
        stored.Path.Should().Be("/es");
    }

    [Fact]
    public async Task Analytics_table_has_no_personal_data_columns()
    {
        using var scope = factory.Services.CreateScope();
        var columns = scope.ServiceProvider.GetRequiredService<AppDbContext>().Model.FindEntityType(typeof(AnalyticsEvent))!
            .GetProperties().Select(p => p.Name.ToLowerInvariant());

        columns.Should().NotContain(c => c.Contains("ip") || c.Contains("agent") || c.Contains("email"));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [InlineData("Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; GPTBot/1.2)")]
    [InlineData("Mozilla/5.0 (compatible; ClaudeBot/1.0)")]
    public async Task Bot_events_are_ignored(string userAgent)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/analytics/events")
        {
            Content = JsonContent.Create(new { type = "PageView", path = "/es", locale = "es", sessionId = Guid.NewGuid() }),
        };
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        (await factory.CreateClient().SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().AnalyticsEvents.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Unknown_event_type_is_rejected()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/public/analytics/events", new { type = "Purchase", path = "/es", locale = "es", sessionId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Summary_reports_current_month_with_change_against_previous_month()
    {
        var returning = Guid.NewGuid();
        var sold = NewProperty("Casa vendida");
        var rented = NewProperty("Casa alquilada");
        await SeedAsync(db =>
        {
            db.AnalyticsEvents.AddRange(
                Event(AnalyticsEventType.PageView, ThisMonth(), returning),
                Event(AnalyticsEventType.PageView, ThisMonth(), returning),
                Event(AnalyticsEventType.PageView, ThisMonth()),
                Event(AnalyticsEventType.PageView, ThisMonth()),
                Event(AnalyticsEventType.PageView, MonthStartUtc().AddDays(-3)),
                Event(AnalyticsEventType.PageView, MonthStartUtc().AddDays(-2)),
                Event(AnalyticsEventType.PropertyView, ThisMonth()),
                Event(AnalyticsEventType.PropertyView, ThisMonth()));
            sold.ChangeStatus(PropertyStatus.Sold, ThisMonth());
            rented.ChangeStatus(PropertyStatus.Rented, ThisMonth());
            db.Properties.AddRange(sold, rented);
        });
        await Admin.PostAsJsonAsync("/api/admin/leads", new { name = "Lead", phone = "+595981000000", interest = "BuyHouse" });

        var summary = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/dashboard/summary");

        summary.GetProperty("siteVisits").GetProperty("value").GetInt32().Should().Be(3);
        summary.GetProperty("siteVisits").GetProperty("previous").GetInt32().Should().Be(2);
        summary.GetProperty("siteVisits").GetProperty("changePercent").GetDecimal().Should().Be(50m);
        summary.GetProperty("propertyViews").GetProperty("value").GetInt32().Should().Be(2);
        summary.GetProperty("propertyViews").GetProperty("changePercent").ValueKind.Should().Be(JsonValueKind.Null);
        summary.GetProperty("sales").GetProperty("value").GetInt32().Should().Be(1);
        summary.GetProperty("monthlySalesGoal").GetInt32().Should().Be(10);
        summary.GetProperty("activeRentals").GetProperty("value").GetInt32().Should().Be(1);
        summary.GetProperty("newLeads").GetProperty("value").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Traffic_returns_one_point_per_day_including_empty_days()
    {
        await SeedAsync(db => db.AnalyticsEvents.Add(Event(AnalyticsEventType.PropertyView, DateTimeOffset.UtcNow.AddMinutes(-1))));

        var traffic = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/dashboard/traffic?days=7");

        traffic.GetArrayLength().Should().Be(7);
        var dates = traffic.EnumerateArray().Select(p => p.GetProperty("date").GetString()).ToList();
        dates.Should().BeInAscendingOrder();
        traffic.EnumerateArray().Sum(p => p.GetProperty("propertyViews").GetInt32()).Should().Be(1);
        traffic.EnumerateArray().Count(p => p.GetProperty("visits").GetInt32() == 0).Should().Be(7);

        (await Admin.GetAsync("/api/admin/dashboard/traffic?days=90")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Admin.GetFromJsonAsync<JsonElement>("/api/admin/dashboard/traffic?days=30")).GetArrayLength().Should().Be(30);
    }

    [Fact]
    public async Task Top_properties_returns_five_most_viewed_in_30_days()
    {
        var properties = Enumerable.Range(1, 8).Select(i => NewProperty($"Casa {i}")).ToList();
        await SeedAsync(db =>
        {
            db.Properties.AddRange(properties);
            for (var i = 0; i < properties.Count; i++)
            {
                for (var v = 0; v <= i; v++)
                {
                    db.AnalyticsEvents.Add(Event(AnalyticsEventType.PropertyView, DateTimeOffset.UtcNow.AddDays(-1), propertyId: properties[i].Id));
                }
            }

            db.AnalyticsEvents.AddRange(Enumerable.Range(0, 20).Select(_ => Event(AnalyticsEventType.PropertyView, DateTimeOffset.UtcNow.AddDays(-40), propertyId: properties[0].Id)));
        });

        var top = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/dashboard/top-properties");

        top.EnumerateArray().Select(p => p.GetProperty("title").GetString()).Should().Equal("Casa 8", "Casa 7", "Casa 6", "Casa 5", "Casa 4");
        top[0].GetProperty("views").GetInt32().Should().Be(8);
    }

    [Fact]
    public async Task Dashboard_requires_permission()
    {
        (await factory.CreateClientWithToken(Permissions.LeadsRead).GetAsync("/api/admin/dashboard/summary")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Retention_removes_events_older_than_13_months()
    {
        await SeedAsync(db => db.AnalyticsEvents.AddRange(
            Event(AnalyticsEventType.PageView, DateTimeOffset.UtcNow.AddMonths(-14)),
            Event(AnalyticsEventType.PageView, DateTimeOffset.UtcNow.AddMonths(-12))));

        using var scope = factory.Services.CreateScope();
        var removed = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PurgeOldAnalyticsEventsCommand());

        removed.Should().Be(1);
        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().AnalyticsEvents.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }
}
