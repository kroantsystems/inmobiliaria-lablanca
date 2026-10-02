using LaBlanca.Domain.Identity;

namespace LaBlanca.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);

    /// <summary>Gasta o mesmo tempo de uma verificação real; usado quando o usuário não existe.</summary>
    void SimulateVerify(string password);
}

public sealed record AccessToken(string Value, string Jti, DateTimeOffset ExpiresAt);

public sealed record GeneratedRefreshToken(string Value, string Hash);

public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }

    AccessToken CreateAccessToken(User user, IReadOnlyList<string> permissions);

    GeneratedRefreshToken CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

public interface IAccessTokenBlacklist
{
    void Revoke(string jti, DateTimeOffset expiresAt);

    bool IsRevoked(string jti);
}

/// <summary>Usuário do access token da requisição atual.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? TokenId { get; }

    DateTimeOffset? TokenExpiresAt { get; }
}
