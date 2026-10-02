using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Common;
using LaBlanca.Application.Features.Properties;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Leads;
using LaBlanca.Domain.Owners;
using LaBlanca.Domain.Properties;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Leads;

public sealed record LeadDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    LeadInterest Interest,
    LeadSource Source,
    LeadStatus Status,
    Guid? PropertyId,
    string? PropertyTitle,
    string? Message,
    string? Notes,
    string Locale,
    DateTimeOffset? ConsentAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

internal static class PhoneRules
{
    public const int MinimumDigits = 8;
    public const int MaximumDigits = 20;

    public static IRuleBuilderOptions<T, string> ContactPhone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(32)
            .Must(p => p is not null && p.Count(char.IsDigit) is >= MinimumDigits and <= MaximumDigits && p.All(c => char.IsDigit(c) || " +-().".Contains(c)))
            .WithMessage(_ => AppMessages.Get(MessageKeys.InvalidPhone));
}

public sealed record SubmitPublicLeadCommand(
    LeadSource Source,
    string Name,
    string Phone,
    string? Email,
    LeadInterest? Interest,
    Guid? PropertyId,
    string? Message,
    string? Locale,
    bool Consent,
    string? Website) : ICommand;

public sealed class SubmitPublicLeadCommandValidator : AbstractValidator<SubmitPublicLeadCommand>
{
    private static readonly LeadSource[] PublicSources = [LeadSource.Contact, LeadSource.VisitRequest, LeadSource.OwnerProposal, LeadSource.Newsletter];

    public SubmitPublicLeadCommandValidator()
    {
        RuleFor(x => x.Source).Must(PublicSources.Contains);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Phone).ContactPhone();
        RuleFor(x => x.Email).NotEmpty().When(x => x.Source == LeadSource.Newsletter);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Message).MaximumLength(2000);
        RuleFor(x => x.Consent).Equal(true).WithMessage(_ => AppMessages.Get(MessageKeys.ConsentRequired));
    }
}

public sealed class SubmitPublicLeadCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<SubmitPublicLeadCommand>
{
    public async Task Handle(SubmitPublicLeadCommand request, CancellationToken cancellationToken)
    {
        // Campo armadilha preenchido = robô: responde como sucesso e não grava nada.
        if (!string.IsNullOrEmpty(request.Website))
        {
            return;
        }

        Guid? propertyId = null;
        if (request.PropertyId is { } id)
        {
            var listed = await db.Properties.AnyAsync(p => p.Id == id && p.IsPublished && PropertyStatuses.Listed.Contains(p.Status), cancellationToken);
            propertyId = listed ? id : null;
        }

        db.Leads.Add(Lead.Create(
            request.Name,
            request.Phone,
            request.Email,
            request.Interest ?? LeadInterest.Other,
            request.Source,
            propertyId,
            request.Message,
            ContentLocale.Resolve(request.Locale),
            clock.GetUtcNow()));
    }
}

public sealed record GetLeadsQuery(int Page = 1, int PageSize = GetLeadsQuery.MaxPageSize) : IQuery<PagedResult<LeadDto>>
{
    public const int MaxPageSize = 500;
}

public sealed class GetLeadsQueryHandler(IAppDbContext db) : IRequestHandler<GetLeadsQuery, PagedResult<LeadDto>>
{
    public async Task<PagedResult<LeadDto>> Handle(GetLeadsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, GetLeadsQuery.MaxPageSize);
        var page = Math.Max(request.Page, 1);
        var total = await db.Leads.CountAsync(cancellationToken);
        var items = await db.Leads.AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadDto(
                l.Id, l.Name, l.Phone, l.Email, l.Interest, l.Source, l.Status, l.PropertyId,
                db.PropertyTranslations.Where(t => t.PropertyId == l.PropertyId && t.Locale == Locales.Default).Select(t => t.Title).FirstOrDefault(),
                l.Message, l.Notes, l.Locale, l.ConsentAt, l.CreatedAt, l.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<LeadDto>(items, total, page, pageSize);
    }
}

public sealed record SaveLeadCommand(Guid? Id, string Name, string Phone, string? Email, LeadInterest Interest, Guid? PropertyId, string? Notes) : ICommand<Guid>;

public sealed class SaveLeadCommandValidator : AbstractValidator<SaveLeadCommand>
{
    public SaveLeadCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Phone).ContactPhone();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Interest).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class SaveLeadCommandHandler(IAppDbContext db) : IRequestHandler<SaveLeadCommand, Guid>
{
    public async Task<Guid> Handle(SaveLeadCommand request, CancellationToken cancellationToken)
    {
        if (request.PropertyId is { } propertyId && !await db.Properties.AnyAsync(p => p.Id == propertyId, cancellationToken))
        {
            throw new BusinessRuleException(MessageKeys.NotFound);
        }

        Lead lead;
        if (request.Id is { } id)
        {
            lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken) ?? throw new NotFoundException();
        }
        else
        {
            lead = Lead.Create(request.Name, request.Phone, request.Email, request.Interest, LeadSource.Manual, request.PropertyId, null, Locales.Default, null);
            db.Leads.Add(lead);
        }

        lead.Update(request.Name, request.Phone, request.Email, request.Interest, request.PropertyId, request.Notes);
        return lead.Id;
    }
}

public sealed record ChangeLeadStatusCommand(Guid Id, LeadStatus Status) : ICommand;

public sealed class ChangeLeadStatusCommandHandler(IAppDbContext db) : IRequestHandler<ChangeLeadStatusCommand>
{
    public async Task Handle(ChangeLeadStatusCommand request, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken) ?? throw new NotFoundException();
        lead.ChangeStatus(request.Status);
    }
}

public sealed record DeleteLeadCommand(Guid Id) : ICommand;

public sealed class DeleteLeadCommandHandler(IAppDbContext db) : IRequestHandler<DeleteLeadCommand>
{
    public async Task Handle(DeleteLeadCommand request, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken) ?? throw new NotFoundException();
        db.Leads.Remove(lead);
    }
}

public sealed record ConvertLeadToOwnerCommand(Guid LeadId) : ICommand<Guid>;

public sealed class ConvertLeadToOwnerCommandHandler(IAppDbContext db) : IRequestHandler<ConvertLeadToOwnerCommand, Guid>
{
    public async Task<Guid> Handle(ConvertLeadToOwnerCommand request, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == request.LeadId, cancellationToken) ?? throw new NotFoundException();
        if (lead.Source != LeadSource.OwnerProposal)
        {
            throw new BusinessRuleException(MessageKeys.LeadNotOwnerProposal);
        }

        var owner = Owner.Create(lead.Name, lead.Phone, lead.Email, null, lead.Message);
        db.Owners.Add(owner);
        lead.ChangeStatus(LeadStatus.Won);
        return owner.Id;
    }
}
