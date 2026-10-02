using System.Net;
using System.Net.Http.Json;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class HealthEndpointTests(ApiFactory factory)
{
    private const string UnreachableDatabase = "Host=127.0.0.1;Port=1;Database=lablanca;Username=postgres;Password=postgres;Timeout=2";

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_return_healthy_when_dependencies_are_available(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<HealthResponse>())!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task Database_down_makes_health_and_ready_unhealthy_but_live_ok()
    {
        await using var unreachable = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ApplyDefaults(UnreachableDatabase, factory.StorageRoot));
        var client = unreachable.CreateClient();

        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var ready = await client.GetAsync("/health/ready");
        ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await ready.Content.ReadFromJsonAsync<HealthResponse>())!.Checks["database"].Should().Be("Unhealthy");
        (await client.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unwritable_storage_makes_ready_unhealthy()
    {
        var file = Path.GetTempFileName();
        try
        {
            await using var broken = factory.WithWebHostBuilder(b => b.UseSetting("Storage:RootPath", Path.Combine(file, "uploads")));

            var response = await broken.CreateClient().GetAsync("/health/ready");

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await response.Content.ReadFromJsonAsync<HealthResponse>())!.Checks["storage"].Should().Be("Unhealthy");
        }
        finally
        {
            File.Delete(file);
        }
    }

    private sealed record HealthResponse(string Status, Dictionary<string, string> Checks);
}
