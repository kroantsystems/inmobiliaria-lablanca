using LaBlanca.Application.Authorization;
using LaBlanca.Domain.Identity;
using LaBlanca.Infrastructure.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LaBlanca.UnitTests.Infrastructure;

public class AuthenticationServicesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "lablanca-api",
        Audience = "lablanca-site",
        SigningKey = "unit-tests-signing-key-0123456789abcdefghijkl",
    };

    [Fact]
    public void Password_hash_uses_bcrypt_with_work_factor_12()
    {
        var hasher = new BCryptPasswordHasher();

        var hash = hasher.Hash("Senha12345");

        hash.Should().MatchRegex(@"^\$2[aby]\$12\$");
        hash.Should().NotContain("Senha12345");
        hasher.Verify("Senha12345", hash).Should().BeTrue();
        hasher.Verify("senha12345", hash).Should().BeFalse();
    }

    [Fact]
    public void Access_token_carries_identity_tenant_role_and_permissions_and_expires_in_15_minutes()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options), new FakeClock(Now));
        var user = User.Create(Guid.NewGuid(), "Leandro", "admin@lablanca.com.py", "hash", Roles.Admin);

        var token = service.CreateAccessToken(user, Roles.PermissionsOf(user.Role));

        var jwt = new JsonWebToken(token.Value);
        jwt.GetClaim("sub").Value.Should().Be(user.Id.ToString());
        jwt.GetClaim("tenant_id").Value.Should().Be(user.TenantId.ToString());
        jwt.GetClaim("role").Value.Should().Be(Roles.Admin);
        jwt.GetClaim("email").Value.Should().Be("admin@lablanca.com.py");
        jwt.GetClaim("name").Value.Should().Be("Leandro");
        jwt.Claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).Should().BeEquivalentTo(Permissions.All);
        jwt.Id.Should().Be(token.Jti).And.NotBeNullOrEmpty();
        token.ExpiresAt.Should().Be(Now.AddMinutes(15));
        jwt.ValidTo.Should().Be(Now.AddMinutes(15).UtcDateTime);
    }

    [Fact]
    public void Short_signing_key_is_rejected()
    {
        var weak = new JwtOptions { Issuer = "i", Audience = "a", SigningKey = "short" };

        var act = () => new JwtTokenService(Microsoft.Extensions.Options.Options.Create(weak), TimeProvider.System);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Refresh_tokens_are_random_and_stored_as_sha256_hash()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options), new FakeClock(Now));

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        first.Value.Should().NotBe(second.Value);
        first.Value.Length.Should().BeGreaterThanOrEqualTo(64);
        first.Hash.Should().MatchRegex("^[0-9a-f]{64}$").And.NotBe(first.Value);
        service.HashRefreshToken(first.Value).Should().Be(first.Hash);
        service.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(7));
    }

    [Fact]
    public void Blacklist_rejects_revoked_jti_until_token_expires()
    {
        var clock = new FakeClock(Now);
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = new CacheClock(clock) });
        var blacklist = new MemoryAccessTokenBlacklist(cache, clock);

        blacklist.Revoke("jti-1", Now.AddMinutes(10));

        blacklist.IsRevoked("jti-1").Should().BeTrue();
        blacklist.IsRevoked("jti-2").Should().BeFalse();
        clock.Advance(TimeSpan.FromMinutes(11));
        blacklist.IsRevoked("jti-1").Should().BeFalse();
    }

#pragma warning disable CS0618 // ISystemClock é a forma de controlar o relógio do MemoryCache.
    private sealed class CacheClock(FakeClock clock) : Microsoft.Extensions.Internal.ISystemClock
    {
        public DateTimeOffset UtcNow => clock.Now;
    }
#pragma warning restore CS0618
}
