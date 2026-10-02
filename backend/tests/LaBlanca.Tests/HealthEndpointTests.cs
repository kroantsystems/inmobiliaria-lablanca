using System.Net;
using System.Net.Http.Json;
using LaBlanca.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.Tests;

public class HealthEndpointTests
{
    [Fact]
    public async Task Health_ReturnsHealthy_WhenDatabaseIsReachable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var efRegistrations = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                        || d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>))
                    .ToList();
                foreach (var descriptor in efRegistrations)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("health-tests"));
            });
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("Healthy", body?.Status);
    }

    [Fact]
    public async Task Health_ReturnsUnhealthy_WhenDatabaseIsUnreachable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=127.0.0.1;Port=1;Database=lablanca;Username=postgres;Password=postgres;Timeout=2");
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("Unhealthy", body?.Status);
    }

    private sealed record HealthResponse(string Status);
}
