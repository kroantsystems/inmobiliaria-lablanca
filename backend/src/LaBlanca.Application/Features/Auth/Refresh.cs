using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Auth;

public sealed record RefreshCommand(string? RefreshToken) : ICommand<AuthOutcome>;

public sealed class RefreshCommandHandler(IAppDbContext db, ITokenService tokens, AuthSessionIssuer issuer, TimeProvider clock)
    : IRequestHandler<RefreshCommand, AuthOutcome>
{
    public async Task<AuthOutcome> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthOutcome.Failed;
        }

        var now = clock.GetUtcNow();
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is null)
        {
            return AuthOutcome.Failed;
        }

        if (stored.RevokedAt is not null)
        {
            // Token já rotacionado sendo reapresentado: possível roubo. Encerra todas as sessões do usuário.
            var active = await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);
            active.ForEach(t => t.Revoke(now));
            return AuthOutcome.Failed;
        }

        if (!stored.IsActive(now))
        {
            return AuthOutcome.Failed;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            stored.Revoke(now);
            return AuthOutcome.Failed;
        }

        var session = issuer.Issue(user);
        stored.Revoke(now, replacedByTokenHash: tokens.HashRefreshToken(session.RefreshToken));
        return new AuthOutcome(session);
    }
}
