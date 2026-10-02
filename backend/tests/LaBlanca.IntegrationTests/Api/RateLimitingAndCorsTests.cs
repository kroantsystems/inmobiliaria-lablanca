using System.Net;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class RateLimitingAndCorsTests(ApiFactory factory)
{
    [Fact]
    public async Task Public_form_above_limit_returns_429_problem()
    {
        await using var limited = factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:PublicForms:PermitLimit", "2"));
        var client = limited.CreateClient();

        (await client.PostAsync("/api/public/_test/form", null)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await client.PostAsync("/api/public/_test/form", null)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        var rejected = await client.PostAsync("/api/public/_test/form", null);

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await rejected.Content.ReadAsStringAsync()).Should().Contain("Muitas requisições");
    }

    [Fact]
    public async Task Allowed_origin_receives_cors_headers_with_credentials()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", TestSettings.FrontendOrigin);

        var response = await factory.CreateClient().SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(TestSettings.FrontendOrigin);
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().ContainSingle("true");
    }

    [Fact]
    public async Task Unknown_origin_receives_no_cors_headers()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await factory.CreateClient().SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
