using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Security;
using LaBlanca.Application.Authorization;
using LaBlanca.Domain.Identity;

namespace LaBlanca.Application.Features.Auth;

public sealed record AuthUserDto(Guid Id, string Name, string Email, string Role, IReadOnlyList<string> Permissions);

/// <summary>Resultado de login/renovação. O refresh token vai só para o cookie, nunca para o corpo da resposta.</summary>
public sealed record AuthSession(string AccessToken, int ExpiresIn, AuthUserDto User, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

/// <summary>Falhas não lançam exceção para que o contador de tentativas e revogações sejam gravados na transação.</summary>
public sealed record AuthOutcome(AuthSession? Session)
{
    public bool Succeeded => Session is not null;

    public static AuthOutcome Failed { get; } = new((AuthSession?)null);
}

public static class AuthUserMapping
{
    public static AuthUserDto ToDto(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role, Roles.PermissionsOf(user.Role));
}

public sealed class AuthSessionIssuer(IAppDbContext db, ITokenService tokens, TimeProvider clock)
{
    public AuthSession Issue(User user)
    {
        var now = clock.GetUtcNow();
        var permissions = Roles.PermissionsOf(user.Role);
        var access = tokens.CreateAccessToken(user, permissions);
        var refresh = tokens.CreateRefreshToken();
        var stored = RefreshToken.Issue(user, refresh.Hash, now, tokens.RefreshTokenLifetime);
        db.RefreshTokens.Add(stored);

        return new AuthSession(
            access.Value,
            (int)Math.Round((access.ExpiresAt - now).TotalSeconds),
            user.ToDto(),
            refresh.Value,
            stored.ExpiresAt);
    }
}
