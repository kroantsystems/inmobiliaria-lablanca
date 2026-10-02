using LaBlanca.Domain.Identity;

namespace LaBlanca.UnitTests.Domain;

public class UserAndTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private static User NewUser() => User.Create(Guid.NewGuid(), "Leandro", "Admin@LaBlanca.com.py ", "hash", "Admin");

    [Fact]
    public void Email_is_normalized()
    {
        NewUser().Email.Should().Be("admin@lablanca.com.py");
    }

    [Fact]
    public void Five_consecutive_failures_lock_the_account_for_15_minutes()
    {
        var user = NewUser();

        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.IsLockedOut(Now).Should().BeFalse();
        user.RegisterFailedLogin(Now);
        user.IsLockedOut(Now).Should().BeTrue();
        user.IsLockedOut(Now.AddMinutes(14)).Should().BeTrue();
        user.IsLockedOut(Now.AddMinutes(15).AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void Successful_login_resets_failure_counter()
    {
        var user = NewUser();
        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.RegisterSuccessfulLogin(Now);
        user.RegisterFailedLogin(Now);

        user.FailedLoginCount.Should().Be(1);
        user.IsLockedOut(Now).Should().BeFalse();
        user.LastLoginAt.Should().Be(Now);
    }

    [Fact]
    public void Refresh_token_is_active_until_expired_or_revoked()
    {
        var user = NewUser();
        var token = RefreshToken.Issue(user, "hash-1", Now, TimeSpan.FromDays(7));

        token.UserId.Should().Be(user.Id);
        token.TenantId.Should().Be(user.TenantId);

        token.IsActive(Now.AddDays(6)).Should().BeTrue();
        token.IsActive(Now.AddDays(7).AddSeconds(1)).Should().BeFalse();

        token.Revoke(Now.AddHours(1), replacedByTokenHash: "hash-2");

        token.IsActive(Now.AddHours(2)).Should().BeFalse();
        token.RevokedAt.Should().Be(Now.AddHours(1));
        token.ReplacedByTokenHash.Should().Be("hash-2");
    }
}
