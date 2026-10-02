using System.Net;
using LaBlanca.Application.Authorization;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class AuthorizationTests(ApiFactory factory)
{
    [Fact]
    public async Task Admin_endpoint_without_token_returns_401_problem()
    {
        var response = await factory.CreateClient().GetAsync("/api/admin/_test");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Public_endpoint_without_token_is_processed()
    {
        var response = await factory.CreateClient().GetAsync("/api/public/_test/not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Token_without_required_permission_returns_403()
    {
        var client = factory.CreateClientWithToken(Permissions.LeadsRead);

        var response = await client.PostAsync("/api/admin/_test", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Token_with_required_permission_is_accepted()
    {
        var client = factory.CreateClientWithToken(Permissions.PropertiesWrite);

        var response = await client.PostAsync("/api/admin/_test", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Expired_token_returns_401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Create([Permissions.PropertiesWrite], expires: DateTime.UtcNow.AddMinutes(-5)));

        var response = await client.GetAsync("/api/admin/_test");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tampered_token_returns_401()
    {
        var token = TestTokens.Create([Permissions.PropertiesWrite]);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token[..^4] + "AAAA");

        var response = await client.GetAsync("/api/admin/_test");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void Only_allowlisted_endpoints_are_anonymous_and_admin_endpoints_declare_a_permission()
    {
        var endpoints = factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(s => s.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => !(e.RoutePattern.RawText ?? string.Empty).Contains("_test"))
            .ToList();

        endpoints.Should().NotBeEmpty();
        foreach (var endpoint in endpoints)
        {
            var route = (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/');

            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            {
                IsPublicRoute(route).Should().BeTrue($"'{route}' is anonymous but not in the public allowlist");
            }

            if (route.StartsWith("api/admin", StringComparison.Ordinal))
            {
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                    .Should().Contain(a => !string.IsNullOrEmpty(a.Policy), $"'{route}' must require a permission policy");
            }
        }
    }

    private static bool IsPublicRoute(string route) =>
        route.StartsWith("api/public/", StringComparison.Ordinal)
        || route is "api/auth/login" or "api/auth/refresh"
        || route.StartsWith("health", StringComparison.Ordinal);
}
