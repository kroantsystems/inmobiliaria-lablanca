using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Common;
using LaBlanca.Domain.Analytics;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Analytics;

/// <summary>Datas de negócio no fuso da imobiliária (Paraguai).</summary>
public static partial class BusinessTime
{
    public const string TimeZoneId = "America/Asuncion";

    public static TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    public static DateTimeOffset MonthStart(DateTimeOffset utcNow, int monthsAgo = 0)
    {
        var local = TimeZoneInfo.ConvertTime(utcNow, Zone);
        return LocalMidnightToUtc(new DateTime(local.Year, local.Month, 1).AddMonths(-monthsAgo));
    }

    public static DateOnly LocalDate(DateTimeOffset utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, Zone).DateTime);

    public static DateTimeOffset LocalMidnightToUtc(DateTime localDate) =>
        new DateTimeOffset(localDate, Zone.GetUtcOffset(localDate)).ToUniversalTime();

    [GeneratedRegex("bot|crawl|spider|slurp|facebookexternalhit|headless|lighthouse|preview", RegexOptions.IgnoreCase)]
    public static partial Regex BotUserAgent();
}

public sealed record RecordEventCommand(
    AnalyticsEventType Type,
    Guid? PropertyId,
    string Path,
    string? Locale,
    Guid SessionId,
    string? Referrer,
    string? UserAgent) : ICommand;

public sealed class RecordEventCommandValidator : AbstractValidator<RecordEventCommand>
{
    public RecordEventCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Path).NotEmpty().MaximumLength(300).Must(p => p.StartsWith('/'));
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Referrer).MaximumLength(2000);
    }
}

public sealed class RecordEventCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<RecordEventCommand>
{
    public async Task Handle(RecordEventCommand request, CancellationToken cancellationToken)
    {
        // O user agent só serve para descartar robôs; nunca é gravado.
        if (string.IsNullOrWhiteSpace(request.UserAgent) || BusinessTime.BotUserAgent().IsMatch(request.UserAgent))
        {
            return;
        }

        Guid? propertyId = null;
        if (request.PropertyId is { } id && await db.Properties.AnyAsync(p => p.Id == id, cancellationToken))
        {
            propertyId = id;
        }

        db.AnalyticsEvents.Add(AnalyticsEvent.Record(
            request.Type,
            propertyId,
            request.Path.Split('?', '#')[0],
            ContentLocale.Resolve(request.Locale),
            request.SessionId,
            ReferrerHost(request.Referrer),
            clock.GetUtcNow()));
    }

    private static string? ReferrerHost(string? referrer)
    {
        if (string.IsNullOrWhiteSpace(referrer))
        {
            return null;
        }

        return Uri.TryCreate(referrer, UriKind.Absolute, out var uri) ? uri.Host : null;
    }
}

public sealed record KpiDto(int Value, int Previous, decimal? ChangePercent)
{
    public static KpiDto From(int value, int previous) =>
        new(value, previous, previous == 0 ? null : Math.Round((value - previous) * 100m / previous, 1));
}

public sealed record DashboardSummaryDto(
    KpiDto SiteVisits,
    KpiDto PropertyViews,
    KpiDto Sales,
    int MonthlySalesGoal,
    KpiDto ActiveRentals,
    KpiDto NewLeads,
    DateTimeOffset MonthStart);

public sealed record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;

public sealed class GetDashboardSummaryQueryHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var monthStart = BusinessTime.MonthStart(now);
        var previousStart = BusinessTime.MonthStart(now, monthsAgo: 1);

        var events = db.AnalyticsEvents.AsNoTracking();
        async Task<int> Sessions(DateTimeOffset from, DateTimeOffset to) =>
            await events.Where(e => e.Type == AnalyticsEventType.PageView && e.OccurredAt >= from && e.OccurredAt < to)
                .Select(e => e.SessionId).Distinct().CountAsync(cancellationToken);
        async Task<int> Views(DateTimeOffset from, DateTimeOffset to) =>
            await events.CountAsync(e => e.Type == AnalyticsEventType.PropertyView && e.OccurredAt >= from && e.OccurredAt < to, cancellationToken);
        async Task<int> Sold(DateTimeOffset from, DateTimeOffset to) =>
            await db.Properties.CountAsync(p => p.SoldAt >= from && p.SoldAt < to, cancellationToken);
        async Task<int> Leads(DateTimeOffset from, DateTimeOffset to) =>
            await db.Leads.CountAsync(l => l.CreatedAt >= from && l.CreatedAt < to, cancellationToken);

        var end = now.AddTicks(1);
        var activeRentals = await db.Properties.CountAsync(p => p.Status == PropertyStatus.Rented, cancellationToken);
        var rentalsBeforeMonth = await db.Properties.CountAsync(p => p.Status == PropertyStatus.Rented && p.RentedAt < monthStart, cancellationToken);
        var goal = await db.SiteSettings.AsNoTracking().Select(s => s.MonthlySalesGoal).FirstOrDefaultAsync(cancellationToken);

        return new DashboardSummaryDto(
            KpiDto.From(await Sessions(monthStart, end), await Sessions(previousStart, monthStart)),
            KpiDto.From(await Views(monthStart, end), await Views(previousStart, monthStart)),
            KpiDto.From(await Sold(monthStart, end), await Sold(previousStart, monthStart)),
            goal,
            KpiDto.From(activeRentals, rentalsBeforeMonth),
            KpiDto.From(await Leads(monthStart, end), await Leads(previousStart, monthStart)),
            monthStart);
    }
}

public sealed record TrafficPointDto(string Date, int Visits, int PropertyViews);

public sealed record GetTrafficQuery(int Days = 7) : IQuery<IReadOnlyList<TrafficPointDto>>;

public sealed class GetTrafficQueryValidator : AbstractValidator<GetTrafficQuery>
{
    public GetTrafficQueryValidator() => RuleFor(x => x.Days).Must(d => d is 7 or 30);
}

public sealed class GetTrafficQueryHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<GetTrafficQuery, IReadOnlyList<TrafficPointDto>>
{
    public async Task<IReadOnlyList<TrafficPointDto>> Handle(GetTrafficQuery request, CancellationToken cancellationToken)
    {
        var today = BusinessTime.LocalDate(clock.GetUtcNow());
        var firstDay = today.AddDays(-(request.Days - 1));
        var from = BusinessTime.LocalMidnightToUtc(firstDay.ToDateTime(TimeOnly.MinValue));

        var events = await db.AnalyticsEvents.AsNoTracking()
            .Where(e => e.OccurredAt >= from && (e.Type == AnalyticsEventType.PageView || e.Type == AnalyticsEventType.PropertyView))
            .Select(e => new { e.Type, e.SessionId, e.OccurredAt })
            .ToListAsync(cancellationToken);

        var byDay = events.GroupBy(e => BusinessTime.LocalDate(e.OccurredAt)).ToDictionary(g => g.Key);
        return
        [
            .. Enumerable.Range(0, request.Days).Select(i =>
            {
                var day = firstDay.AddDays(i);
                var items = byDay.GetValueOrDefault(day);
                return new TrafficPointDto(
                    day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    items?.Where(e => e.Type == AnalyticsEventType.PageView).Select(e => e.SessionId).Distinct().Count() ?? 0,
                    items?.Count(e => e.Type == AnalyticsEventType.PropertyView) ?? 0);
            }),
        ];
    }
}

public sealed record TopPropertyDto(Guid Id, string Title, int Views);

public sealed record GetTopPropertiesQuery(int Take = 5, int Days = 30) : IQuery<IReadOnlyList<TopPropertyDto>>;

public sealed class GetTopPropertiesQueryHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<GetTopPropertiesQuery, IReadOnlyList<TopPropertyDto>>
{
    public async Task<IReadOnlyList<TopPropertyDto>> Handle(GetTopPropertiesQuery request, CancellationToken cancellationToken)
    {
        var from = clock.GetUtcNow().AddDays(-request.Days);
        var top = await db.AnalyticsEvents.AsNoTracking()
            .Where(e => e.Type == AnalyticsEventType.PropertyView && e.PropertyId != null && e.OccurredAt >= from)
            .GroupBy(e => e.PropertyId!.Value)
            .Select(g => new { PropertyId = g.Key, Views = g.Count() })
            .OrderByDescending(x => x.Views)
            .Take(Math.Clamp(request.Take, 1, 20))
            .ToListAsync(cancellationToken);

        var ids = top.Select(t => t.PropertyId).ToList();
        var titles = await db.PropertyTranslations.AsNoTracking()
            .Where(t => ids.Contains(t.PropertyId) && t.Locale == Locales.Default)
            .ToDictionaryAsync(t => t.PropertyId, t => t.Title, cancellationToken);

        return [.. top.Select(t => new TopPropertyDto(t.PropertyId, titles.GetValueOrDefault(t.PropertyId) ?? string.Empty, t.Views))];
    }
}

/// <summary>Remove eventos brutos com mais de 13 meses, de todos os tenants.</summary>
public sealed record PurgeOldAnalyticsEventsCommand : ICommand<int>
{
    public const int RetentionMonths = 13;
}

public sealed class PurgeOldAnalyticsEventsCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<PurgeOldAnalyticsEventsCommand, int>
{
    public async Task<int> Handle(PurgeOldAnalyticsEventsCommand request, CancellationToken cancellationToken)
    {
        var cutoff = clock.GetUtcNow().AddMonths(-PurgeOldAnalyticsEventsCommand.RetentionMonths);
        return await db.AnalyticsEvents.IgnoreQueryFilters().Where(e => e.OccurredAt < cutoff).ExecuteDeleteAsync(cancellationToken);
    }
}
