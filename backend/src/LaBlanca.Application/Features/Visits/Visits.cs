using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Visits;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Visits;

public sealed record VisitDto(
    Guid Id,
    Guid PropertyId,
    string? PropertyTitle,
    Guid? LeadId,
    string ClientName,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    DateTimeOffset EndsAt,
    string? Notes,
    VisitStatus Status);

internal static class VisitProjection
{
    public static IQueryable<VisitDto> ToDtos(this IQueryable<Visit> visits, IAppDbContext db) =>
        visits.Select(v => new VisitDto(
            v.Id,
            v.PropertyId,
            db.PropertyTranslations.Where(t => t.PropertyId == v.PropertyId && t.Locale == Locales.Default).Select(t => t.Title).FirstOrDefault(),
            v.LeadId,
            v.ClientName,
            v.StartsAt,
            v.DurationMinutes,
            v.StartsAt.AddMinutes(v.DurationMinutes),
            v.Notes,
            v.Status));
}

public sealed record GetVisitsQuery(DateTimeOffset From, DateTimeOffset To) : IQuery<IReadOnlyList<VisitDto>>
{
    public const int MaxRangeDays = 62;
}

public sealed class GetVisitsQueryValidator : AbstractValidator<GetVisitsQuery>
{
    public GetVisitsQueryValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From);
        RuleFor(x => x).Must(x => x.To - x.From <= TimeSpan.FromDays(GetVisitsQuery.MaxRangeDays))
            .OverridePropertyName("to")
            .WithMessage(_ => AppMessages.Get(MessageKeys.VisitRangeTooLarge, GetVisitsQuery.MaxRangeDays));
    }
}

public sealed class GetVisitsQueryHandler(IAppDbContext db) : IRequestHandler<GetVisitsQuery, IReadOnlyList<VisitDto>>
{
    public async Task<IReadOnlyList<VisitDto>> Handle(GetVisitsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From.ToUniversalTime();
        var to = request.To.ToUniversalTime();
        return await db.Visits.AsNoTracking()
            .Where(v => v.StartsAt >= from && v.StartsAt < to)
            .OrderBy(v => v.StartsAt)
            .ToDtos(db)
            .ToListAsync(cancellationToken);
    }
}

public sealed record GetUpcomingVisitsQuery(int Take = 10) : IQuery<IReadOnlyList<VisitDto>>;

public sealed class GetUpcomingVisitsQueryHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<GetUpcomingVisitsQuery, IReadOnlyList<VisitDto>>
{
    public async Task<IReadOnlyList<VisitDto>> Handle(GetUpcomingVisitsQuery request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return await db.Visits.AsNoTracking()
            .Where(v => v.Status == VisitStatus.Scheduled && v.StartsAt >= now)
            .OrderBy(v => v.StartsAt)
            .Take(Math.Clamp(request.Take, 1, 50))
            .ToDtos(db)
            .ToListAsync(cancellationToken);
    }
}

public sealed record SaveVisitCommand(Guid? Id, Guid PropertyId, Guid? LeadId, string? ClientName, DateTimeOffset StartsAt, int? DurationMinutes, string? Notes)
    : ICommand<Guid>;

public sealed class SaveVisitCommandValidator : AbstractValidator<SaveVisitCommand>
{
    public SaveVisitCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.ClientName).NotEmpty().MaximumLength(120).When(x => x.LeadId is null);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 480);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class SaveVisitCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<SaveVisitCommand, Guid>
{
    public async Task<Guid> Handle(SaveVisitCommand request, CancellationToken cancellationToken)
    {
        var startsAt = request.StartsAt.ToUniversalTime();
        var duration = request.DurationMinutes ?? Visit.DefaultDurationMinutes;

        Visit? visit = null;
        if (request.Id is { } id)
        {
            visit = await db.Visits.FirstOrDefaultAsync(v => v.Id == id, cancellationToken) ?? throw new NotFoundException();
        }

        if ((visit is null || visit.StartsAt != startsAt) && startsAt <= clock.GetUtcNow())
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["startsAt"] = [AppMessages.Get(MessageKeys.VisitDateInPast)] });
        }

        if (!await db.Properties.AnyAsync(p => p.Id == request.PropertyId, cancellationToken))
        {
            throw new BusinessRuleException(MessageKeys.NotFound);
        }

        var lead = request.LeadId is { } leadId
            ? await db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken) ?? throw new BusinessRuleException(MessageKeys.NotFound)
            : null;
        var clientName = string.IsNullOrWhiteSpace(request.ClientName) ? lead!.Name : request.ClientName;

        var end = startsAt.AddMinutes(duration);
        var candidates = await db.Visits
            .Where(v => v.PropertyId == request.PropertyId && v.Status == VisitStatus.Scheduled && v.Id != request.Id && v.StartsAt < end)
            .ToListAsync(cancellationToken);
        if (candidates.Any(v => v.Overlaps(startsAt, duration)))
        {
            throw new ConflictException(MessageKeys.VisitConflict);
        }

        if (visit is null)
        {
            visit = Visit.Schedule(request.PropertyId, request.LeadId, clientName, startsAt, duration, request.Notes);
            db.Visits.Add(visit);
        }
        else
        {
            visit.Reschedule(request.PropertyId, request.LeadId, clientName, startsAt, duration, request.Notes);
        }

        lead?.MarkVisitScheduled();
        return visit.Id;
    }
}

public sealed record ChangeVisitStatusCommand(Guid Id, VisitStatus Status) : ICommand;

public sealed class ChangeVisitStatusCommandHandler(IAppDbContext db) : IRequestHandler<ChangeVisitStatusCommand>
{
    public async Task Handle(ChangeVisitStatusCommand request, CancellationToken cancellationToken)
    {
        var visit = await db.Visits.FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken) ?? throw new NotFoundException();
        switch (request.Status)
        {
            case VisitStatus.Done:
                visit.Complete();
                break;
            case VisitStatus.Cancelled:
                visit.Cancel();
                break;
            case VisitStatus.NoShow:
                visit.MarkNoShow();
                break;
            default:
                throw new BusinessRuleException(MessageKeys.Validation);
        }
    }
}

public sealed record GetVisitQuery(Guid Id) : IQuery<VisitDto>;

public sealed class GetVisitQueryHandler(IAppDbContext db) : IRequestHandler<GetVisitQuery, VisitDto>
{
    public async Task<VisitDto> Handle(GetVisitQuery request, CancellationToken cancellationToken) =>
        await db.Visits.AsNoTracking().Where(v => v.Id == request.Id).ToDtos(db).FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException();
}
