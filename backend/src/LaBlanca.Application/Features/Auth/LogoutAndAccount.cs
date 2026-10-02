using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Security;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Auth;

public sealed record LogoutCommand(string? RefreshToken) : ICommand;

public sealed class LogoutCommandHandler(IAppDbContext db, ITokenService tokens, ICurrentUser currentUser, IAccessTokenBlacklist blacklist)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hash = tokens.HashRefreshToken(request.RefreshToken);
            var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == currentUser.UserId, cancellationToken);
            if (stored is not null)
            {
                db.RefreshTokens.Remove(stored);
            }
        }

        if (currentUser.TokenId is { } jti && currentUser.TokenExpiresAt is { } expiresAt)
        {
            blacklist.Revoke(jti, expiresAt);
        }
    }
}

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<AuthSession>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).StrongPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage(_ => AppMessages.Get(MessageKeys.PasswordMustDiffer));
    }
}

public sealed class ChangePasswordCommandHandler(
    IAppDbContext db,
    IPasswordHasher hasher,
    ICurrentUser currentUser,
    IAccessTokenBlacklist blacklist,
    AuthSessionIssuer issuer,
    TimeProvider clock) : IRequestHandler<ChangePasswordCommand, AuthSession>
{
    public async Task<AuthSession> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedException();

        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessRuleException(MessageKeys.CurrentPasswordInvalid);
        }

        user.ChangePasswordHash(hasher.Hash(request.NewPassword));

        var now = clock.GetUtcNow();
        var sessions = await db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(cancellationToken);
        sessions.ForEach(t => t.Revoke(now));

        if (currentUser.TokenId is { } jti && currentUser.TokenExpiresAt is { } expiresAt)
        {
            blacklist.Revoke(jti, expiresAt);
        }

        return issuer.Issue(user);
    }
}

public sealed record GetCurrentUserQuery : IQuery<AuthUserDto>;

public sealed class GetCurrentUserQueryHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetCurrentUserQuery, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUser.UserId && u.IsActive, cancellationToken)
            ?? throw new UnauthorizedException();
        return user.ToDto();
    }
}

public static class PasswordRules
{
    public const int MinimumLength = 10;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(200)
            .Must(p => p is not null && p.Length >= MinimumLength && p.Any(char.IsLetter) && p.Any(char.IsDigit))
            .WithMessage(_ => AppMessages.Get(MessageKeys.PasswordPolicy, MinimumLength));
}
