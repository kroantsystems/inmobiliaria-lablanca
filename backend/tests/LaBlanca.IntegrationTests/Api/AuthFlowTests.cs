using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaBlanca.Application.Features.Auth;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.IntegrationTests.Infrastructure;
using LaBlanca.Shared.Errors;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static LaBlanca.IntegrationTests.Infrastructure.AuthTestHelpers;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class AuthFlowTests(ApiFactory factory) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateAdminAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_returns_access_token_in_body_and_refresh_token_only_in_secure_cookie()
    {
        var response = await factory.CreateClient().LoginAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("expiresIn").GetInt32().Should().Be(900);
        json.GetProperty("user").GetProperty("email").GetString().Should().Be(AdminEmail);
        json.GetProperty("user").GetProperty("permissions").GetArrayLength().Should().BeGreaterThan(0);

        var cookie = RefreshCookieHeader(response)!.ToLowerInvariant();
        cookie.Should().Contain("httponly").And.Contain("secure").And.Contain("samesite=strict").And.Contain("path=/api/auth").And.Contain("expires=");
        body.Should().NotContain(RefreshCookieValue(response)!);
    }

    [Fact]
    public async Task Refresh_token_is_stored_only_as_hash()
    {
        var (_, refresh) = await factory.CreateClient().LoginAndReadAsync();

        using var scope = factory.Services.CreateScope();
        var hashes = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RefreshTokens.IgnoreQueryFilters()
            .Select(t => t.TokenHash).ToListAsync();
        hashes.Should().ContainSingle().Which.Should().NotBe(refresh).And.MatchRegex("^[0-9a-f]{64}$");
    }

    [Theory]
    [InlineData(AdminEmail, "SenhaErrada123")]
    [InlineData("ninguem@lablanca.com.py", AdminPassword)]
    public async Task Invalid_credentials_return_the_same_generic_401(string email, string password)
    {
        var response = await factory.CreateClient().LoginAsync(email, password);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        problem.GetProperty("detail").GetString().Should().Be("E-mail ou senha inválidos.");
        RefreshCookieHeader(response).Should().BeNull();
    }

    [Fact]
    public async Task Missing_login_data_returns_validation_errors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "invalido", password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
        errors.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("email", "password");
    }

    [Fact]
    public async Task Account_is_locked_after_five_wrong_passwords()
    {
        var client = factory.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            (await client.LoginAsync(password: "SenhaErrada123")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await client.LoginAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inactive_user_cannot_login()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync();
            user.Deactivate();
            await db.SaveChangesAsync();
        }

        (await factory.CreateClient().LoginAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Full_session_flow_with_rotation_reuse_detection_and_logout()
    {
        var client = factory.CreateClient();
        var (access, refresh) = await client.LoginAndReadAsync();

        var me = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/auth/me").WithBearer(access));
        me.StatusCode.Should().Be(HttpStatusCode.OK);

        var rotated = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh").WithRefreshCookie(refresh));
        rotated.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAccess = await ReadAccessTokenAsync(rotated);
        var newRefresh = RefreshCookieValue(rotated)!;
        newRefresh.Should().NotBe(refresh);

        var reused = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh").WithRefreshCookie(refresh));
        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        RefreshCookieHeader(reused)!.ToLowerInvariant().Should().Contain("expires=thu, 01 jan 1970");

        var afterTheft = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh").WithRefreshCookie(newRefresh));
        afterTheft.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "reuse revokes every session of the user");

        var logout = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout").WithBearer(newAccess));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterLogout = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/auth/me").WithBearer(newAccess));
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_removes_refresh_token_from_database()
    {
        var client = factory.CreateClient();
        var (access, refresh) = await client.LoginAndReadAsync();

        var logout = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout").WithBearer(access).WithRefreshCookie(refresh));

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().RefreshTokens.IgnoreQueryFilters().CountAsync()).Should().Be(0);
        (await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh").WithRefreshCookie(refresh)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_without_cookie_returns_401()
    {
        (await factory.CreateClient().PostAsync("/api/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_password_validates_current_and_policy_then_rotates_sessions()
    {
        var client = factory.CreateClient();
        var (access, refresh) = await client.LoginAndReadAsync();

        var wrongCurrent = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = "Errada12345", newPassword = "NovaSenha2027" }),
        }.WithBearer(access));
        wrongCurrent.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wrongCurrent.Content.ReadAsStringAsync()).Should().Contain("A senha atual está incorreta.");

        var weak = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = AdminPassword, newPassword = "curta" }),
        }.WithBearer(access));
        weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        JsonDocument.Parse(await weak.Content.ReadAsStringAsync()).RootElement.GetProperty("errors").TryGetProperty("newPassword", out _).Should().BeTrue();

        var changed = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = AdminPassword, newPassword = "NovaSenha2027" }),
        }.WithBearer(access));
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        RefreshCookieValue(changed).Should().NotBeNullOrEmpty();

        (await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/auth/me").WithBearer(access))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh").WithRefreshCookie(refresh))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.LoginAsync(password: AdminPassword)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.LoginAsync(password: "NovaSenha2027")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task There_is_no_public_registration_endpoint()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new { email = "x@y.com", password = "Qualquer12345" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_is_limited_to_five_requests_per_minute_per_ip()
    {
        await using var limited = factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:Login:PermitLimit", "5"));
        var client = limited.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            (await client.LoginAsync(password: "SenhaErrada123")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await client.LoginAsync()).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Admin_tool_rejects_duplicate_email_and_resets_password()
    {
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var duplicate = () => sender.Send(new CreateAdminCommand("la-blanca", "Outro", AdminEmail.ToUpperInvariant(), "OutraSenha123"));
        await duplicate.Should().ThrowAsync<ConflictException>();

        await sender.Send(new ResetPasswordCommand("la-blanca", AdminEmail, "Redefinida2026"));
        (await factory.CreateClient().LoginAsync(password: "Redefinida2026")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
