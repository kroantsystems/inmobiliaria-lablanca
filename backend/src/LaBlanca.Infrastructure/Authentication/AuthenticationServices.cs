using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LaBlanca.Application.Abstractions.Security;
using LaBlanca.Application.Authorization;
using LaBlanca.Domain.Identity;
using LaBlanca.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LaBlanca.Infrastructure.Authentication;

internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    private static readonly Lazy<string> DummyHash = new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor));

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    public void SimulateVerify(string password) => BCrypt.Net.BCrypt.Verify(password, DummyHash.Value);
}

internal sealed class JwtTokenService : ITokenService
{
    private const int RefreshTokenBytes = 64;

    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
        if (Encoding.UTF8.GetByteCount(_options.SigningKey) < StartupConfigurationValidator.MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException($"Jwt:SigningKey must have at least {StartupConfigurationValidator.MinimumSigningKeyBytes} bytes.");
        }

        _credentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken CreateAccessToken(User user, IReadOnlyList<string> permissions)
    {
        var now = _clock.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.Name),
            new("tenant_id", user.TenantId.ToString()),
            new("role", user.Role),
        };
        claims.AddRange(permissions.Select(p => new Claim(Permissions.ClaimType, p)));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _credentials,
        });

        return new AccessToken(token, jti, expiresAt);
    }

    public GeneratedRefreshToken CreateRefreshToken()
    {
        var value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        return new GeneratedRefreshToken(value, HashRefreshToken(value));
    }

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}

internal sealed class MemoryAccessTokenBlacklist(IMemoryCache cache, TimeProvider clock) : IAccessTokenBlacklist
{
    public void Revoke(string jti, DateTimeOffset expiresAt)
    {
        if (expiresAt > clock.GetUtcNow())
        {
            cache.Set(Key(jti), true, expiresAt);
        }
    }

    public bool IsRevoked(string jti) => cache.TryGetValue(Key(jti), out _);

    private static string Key(string jti) => $"revoked-access-token:{jti}";
}

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    public string? TokenId => Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

    public DateTimeOffset? TokenExpiresAt =>
        long.TryParse(Principal?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp)
            : null;
}
