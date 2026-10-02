using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LaBlanca.Application.Features.Auth;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.IntegrationTests.Infrastructure;

public static class AuthTestHelpers
{
    public const string AdminEmail = "admin@lablanca.com.py";
    public const string AdminPassword = "LaBlanca2026";

    public static async Task<Guid> CreateAdminAsync(this ApiFactory factory, string email = AdminEmail, string password = AdminPassword)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateAdminCommand("la-blanca", "Leandro", email, password));
    }

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email = AdminEmail, string password = AdminPassword) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<(string AccessToken, string RefreshCookie)> LoginAndReadAsync(this HttpClient client, string email = AdminEmail, string password = AdminPassword)
    {
        var response = await client.LoginAsync(email, password);
        response.EnsureSuccessStatusCode();
        return (await ReadAccessTokenAsync(response), RefreshCookieValue(response)!);
    }

    public static async Task<string> ReadAccessTokenAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString()!;

    public static string? RefreshCookieHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith("lb_rt=", StringComparison.Ordinal))
            : null;

    public static string? RefreshCookieValue(HttpResponseMessage response)
    {
        var header = RefreshCookieHeader(response);
        return header?.Split(';')[0]["lb_rt=".Length..];
    }

    public static HttpRequestMessage WithRefreshCookie(this HttpRequestMessage request, string cookieValue)
    {
        request.Headers.Add("Cookie", $"lb_rt={cookieValue}");
        return request;
    }

    public static HttpRequestMessage WithBearer(this HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
