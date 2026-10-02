using LaBlanca.Domain.Common;

namespace LaBlanca.Domain.Visits;

public enum VisitStatus
{
    Scheduled,
    Done,
    Cancelled,
    NoShow,
}

public sealed class Visit : TenantEntity, IAuditable
{
    public const int DefaultDurationMinutes = 60;

    private Visit()
    {
    }

    public Guid PropertyId { get; private set; }

    public Guid? LeadId { get; private set; }

    public string ClientName { get; private set; } = string.Empty;

    public DateTimeOffset StartsAt { get; private set; }

    public int DurationMinutes { get; private set; } = DefaultDurationMinutes;

    public string? Notes { get; private set; }

    public VisitStatus Status { get; private set; } = VisitStatus.Scheduled;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset EndsAt => StartsAt.AddMinutes(DurationMinutes);

    public static Visit Schedule(Guid propertyId, Guid? leadId, string clientName, DateTimeOffset startsAt, int durationMinutes, string? notes)
    {
        var visit = new Visit();
        visit.Reschedule(propertyId, leadId, clientName, startsAt, durationMinutes, notes);
        return visit;
    }

    public void Reschedule(Guid propertyId, Guid? leadId, string clientName, DateTimeOffset startsAt, int durationMinutes, string? notes)
    {
        PropertyId = propertyId;
        LeadId = leadId;
        ClientName = clientName.Trim();
        StartsAt = startsAt.ToUniversalTime();
        DurationMinutes = durationMinutes;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public bool Overlaps(DateTimeOffset startsAt, int durationMinutes) =>
        Status == VisitStatus.Scheduled && startsAt < EndsAt && StartsAt < startsAt.AddMinutes(durationMinutes);

    public void Complete() => Status = VisitStatus.Done;

    public void Cancel() => Status = VisitStatus.Cancelled;

    public void MarkNoShow() => Status = VisitStatus.NoShow;
}
