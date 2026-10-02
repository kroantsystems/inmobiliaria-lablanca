using LaBlanca.Application.Abstractions.Security;
using LaBlanca.Application.Authorization;
using LaBlanca.Application.Features.Auth;
using LaBlanca.Domain.Identity;
using LaBlanca.Infrastructure.Authentication;
using LaBlanca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LaBlanca.UnitTests.Application.Auth;

public class AuthHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
    private const string Password = "Correta12345";

    private readonly FakeClock _clock = new(Now);
    private readonly BCryptPasswordHasher _hasher = new();
    private readonly AppDbContext _db = InMemoryDb.Create();
    private readonly JwtTokenService _tokens;

    public AuthHandlersTests()
    {
        _tokens = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "i",
            Audience = "a",
            SigningKey = "unit-tests-signing-key-0123456789abcdefghijkl",
        }), _clock);
    }

    private async Task<User> SeedUserAsync(bool active = true)
    {
        var user = User.Create(InMemoryDb.TenantId, "Leandro", "admin@lablanca.com.py", _hasher.Hash(Password), Roles.Admin);
        if (!active)
        {
            user.Deactivate();
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private LoginCommandHandler LoginHandler() => new(_db, _hasher, new AuthSessionIssuer(_db, _tokens, _clock), _clock);

    [Fact]
    public async Task Login_with_valid_credentials_issues_tokens_and_stores_only_refresh_hash()
    {
        await SeedUserAsync();

        var outcome = await LoginHandler().Handle(new LoginCommand(" Admin@LaBlanca.com.py ", Password), CancellationToken.None);
        await _db.SaveChangesAsync();

        outcome.Succeeded.Should().BeTrue();
        outcome.Session!.AccessToken.Should().NotBeNullOrEmpty();
        outcome.Session.ExpiresIn.Should().Be(900);
        outcome.Session.User.Email.Should().Be("admin@lablanca.com.py");
        var stored = await _db.RefreshTokens.SingleAsync();
        stored.TokenHash.Should().Be(_tokens.HashRefreshToken(outcome.Session.RefreshToken)).And.NotBe(outcome.Session.RefreshToken);
    }

    [Theory]
    [InlineData("admin@lablanca.com.py", "Errada12345")]
    [InlineData("nobody@lablanca.com.py", Password)]
    public async Task Login_with_wrong_email_or_password_fails(string email, string password)
    {
        await SeedUserAsync();

        var outcome = await LoginHandler().Handle(new LoginCommand(email, password), CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        outcome.Session.Should().BeNull();
    }

    [Fact]
    public async Task Inactive_user_cannot_login()
    {
        await SeedUserAsync(active: false);

        var outcome = await LoginHandler().Handle(new LoginCommand("admin@lablanca.com.py", Password), CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Locked_user_cannot_login_even_with_correct_password()
    {
        var user = await SeedUserAsync();
        var handler = LoginHandler();
        for (var i = 0; i < User.MaxFailedLogins; i++)
        {
            await handler.Handle(new LoginCommand(user.Email, "Errada12345"), CancellationToken.None);
        }

        var outcome = await handler.Handle(new LoginCommand(user.Email, Password), CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        user.IsLockedOut(Now).Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_rotates_token_and_reuse_revokes_every_session()
    {
        var user = await SeedUserAsync();
        var login = await LoginHandler().Handle(new LoginCommand(user.Email, Password), CancellationToken.None);
        await _db.SaveChangesAsync();
        var refresh = new RefreshCommandHandler(_db, _tokens, new AuthSessionIssuer(_db, _tokens, _clock), _clock);

        var rotated = await refresh.Handle(new RefreshCommand(login.Session!.RefreshToken), CancellationToken.None);
        await _db.SaveChangesAsync();

        rotated.Succeeded.Should().BeTrue();
        rotated.Session!.RefreshToken.Should().NotBe(login.Session.RefreshToken);

        var reused = await refresh.Handle(new RefreshCommand(login.Session.RefreshToken), CancellationToken.None);
        await _db.SaveChangesAsync();

        reused.Succeeded.Should().BeFalse();
        (await _db.RefreshTokens.CountAsync(t => t.RevokedAt == null)).Should().Be(0);
        (await refresh.Handle(new RefreshCommand(rotated.Session.RefreshToken), CancellationToken.None)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Expired_refresh_token_fails()
    {
        var user = await SeedUserAsync();
        var login = await LoginHandler().Handle(new LoginCommand(user.Email, Password), CancellationToken.None);
        await _db.SaveChangesAsync();
        _clock.Advance(TimeSpan.FromDays(8));

        var outcome = await new RefreshCommandHandler(_db, _tokens, new AuthSessionIssuer(_db, _tokens, _clock), _clock)
            .Handle(new RefreshCommand(login.Session!.RefreshToken), CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
    }
}

public class AuthValidatorsTests
{
    [Theory]
    [InlineData("", "x")]
    [InlineData("not-an-email", "x")]
    [InlineData("a@b.com", "")]
    public void Login_requires_valid_email_and_password(string email, string password)
    {
        new LoginCommandValidator().Validate(new LoginCommand(email, password)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("curta1", false)]
    [InlineData("semnumerosaqui", false)]
    [InlineData("1234567890", false)]
    [InlineData("Atual12345", false)]
    [InlineData("NovaSenha2026", true)]
    public void New_password_needs_10_chars_letters_numbers_and_differ_from_current(string newPassword, bool valid)
    {
        var result = new ChangePasswordCommandValidator().Validate(new ChangePasswordCommand("Atual12345", newPassword));

        result.IsValid.Should().Be(valid);
    }
}
