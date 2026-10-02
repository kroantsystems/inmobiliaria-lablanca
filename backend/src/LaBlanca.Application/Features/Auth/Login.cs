using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Security;
using LaBlanca.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthOutcome>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public sealed class LoginCommandHandler(IAppDbContext db, IPasswordHasher hasher, AuthSessionIssuer issuer, TimeProvider clock)
    : IRequestHandler<LoginCommand, AuthOutcome>
{
    public async Task<AuthOutcome> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // Mesmo custo de uma verificação real: o tempo de resposta não revela quais e-mails existem.
            hasher.SimulateVerify(request.Password);
            return AuthOutcome.Failed;
        }

        if (!user.IsActive || user.IsLockedOut(now))
        {
            return AuthOutcome.Failed;
        }

        if (!hasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(now);
            return AuthOutcome.Failed;
        }

        user.RegisterSuccessfulLogin(now);
        return new AuthOutcome(issuer.Issue(user));
    }
}
