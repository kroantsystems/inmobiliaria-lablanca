using LaBlanca.Api.Configuration;
using LaBlanca.Application.Features.Auth;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LaBlanca.Api.Controllers;

public sealed record AuthResponse(string AccessToken, int ExpiresIn, AuthUserDto User);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    public const string RefreshCookieName = "lb_rt";
    public const string RefreshCookiePath = "/api/auth";

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var outcome = await sender.Send(command, cancellationToken);
        return outcome.Succeeded
            ? Ok(StartSession(outcome.Session!))
            : Problem(statusCode: StatusCodes.Status401Unauthorized, detail: AppMessages.Get(MessageKeys.InvalidCredentials));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var outcome = await sender.Send(new RefreshCommand(Request.Cookies[RefreshCookieName]), cancellationToken);
        if (outcome.Succeeded)
        {
            return Ok(StartSession(outcome.Session!));
        }

        // Problem em vez de exceção: o tratamento de exceções limparia o Set-Cookie que remove o cookie.
        DeleteRefreshCookie();
        return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: AppMessages.Get(MessageKeys.SessionExpired));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(Request.Cookies[RefreshCookieName]), cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<AuthResponse>> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        Ok(StartSession(await sender.Send(command, cancellationToken)));

    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCurrentUserQuery(), cancellationToken));

    private AuthResponse StartSession(AuthSession session)
    {
        Response.Cookies.Append(RefreshCookieName, session.RefreshToken, CookieOptions(session.RefreshTokenExpiresAt));
        return new AuthResponse(session.AccessToken, session.ExpiresIn, session.User);
    }

    private void DeleteRefreshCookie() => Response.Cookies.Delete(RefreshCookieName, CookieOptions(expires: null));

    private static CookieOptions CookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = RefreshCookiePath,
        Expires = expires,
        IsEssential = true,
    };
}
