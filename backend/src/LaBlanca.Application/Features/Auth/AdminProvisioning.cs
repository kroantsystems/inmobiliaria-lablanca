using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Security;
using LaBlanca.Application.Authorization;
using LaBlanca.Domain.Identity;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Auth;

// Usados apenas pela ferramenta de linha de comando (LaBlanca.Tools); não há endpoint de cadastro.
public sealed record CreateAdminCommand(string TenantSlug, string Name, string Email, string Password) : ICommand<Guid>;

public sealed class CreateAdminCommandValidator : AbstractValidator<CreateAdminCommand>
{
    public CreateAdminCommandValidator()
    {
        RuleFor(x => x.TenantSlug).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).StrongPassword();
    }
}

public sealed class CreateAdminCommandHandler(IAppDbContext db, IPasswordHasher hasher) : IRequestHandler<CreateAdminCommand, Guid>
{
    public async Task<Guid> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == request.TenantSlug, cancellationToken)
            ?? throw new NotFoundException();

        var email = User.NormalizeEmail(request.Email);
        var exists = await db.Users.IgnoreQueryFilters().AnyAsync(u => u.TenantId == tenant.Id && u.Email == email, cancellationToken);
        if (exists)
        {
            throw new ConflictException(MessageKeys.UserEmailExists);
        }

        var user = User.Create(tenant.Id, request.Name, email, hasher.Hash(request.Password), Roles.Admin);
        db.Users.Add(user);
        return user.Id;
    }
}

public sealed record ResetPasswordCommand(string TenantSlug, string Email, string NewPassword) : ICommand;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.TenantSlug).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}

public sealed class ResetPasswordCommandHandler(IAppDbContext db, IPasswordHasher hasher, TimeProvider clock) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.IgnoreQueryFilters()
            .Join(db.Tenants.Where(t => t.Slug == request.TenantSlug), u => u.TenantId, t => t.Id, (u, _) => u)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            ?? throw new NotFoundException();

        user.ChangePasswordHash(hasher.Hash(request.NewPassword));
        user.RegisterSuccessfulLogin(clock.GetUtcNow());

        var now = clock.GetUtcNow();
        var sessions = await db.RefreshTokens.IgnoreQueryFilters().Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(cancellationToken);
        sessions.ForEach(t => t.Revoke(now));
    }
}
