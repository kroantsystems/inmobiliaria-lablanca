using System.Net;
using System.Net.Http.Json;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class HealthEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_returns_healthy_when_database_is_reachable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_returns_unhealthy_when_database_is_unreachable()
    {
        await using var unreachable = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=127.0.0.1;Port=1;Database=lablanca;Username=postgres;Password=postgres;Timeout=2");
        });
        var client = unreachable.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("Unhealthy");
    }

    private sealed record HealthResponse(string Status);
}
