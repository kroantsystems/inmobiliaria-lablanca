using LaBlanca.Domain.Common;

namespace LaBlanca.Domain.Leads;

public enum LeadSource
{
    Contact,
    VisitRequest,
    OwnerProposal,
    Newsletter,
    Manual,
}

public enum LeadStatus
{
    New,
    Contacted,
    VisitScheduled,
    Negotiating,
    Won,
    Lost,
}

public enum LeadInterest
{
    BuyHouse,
    RentApartment,
    BuyLand,
    Other,
}

public sealed class Lead : TenantEntity, IAuditable
{
    private Lead()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public LeadInterest Interest { get; private set; }

    public LeadSource Source { get; private set; }

    public LeadStatus Status { get; private set; } = LeadStatus.New;

    public Guid? PropertyId { get; private set; }

    public string? Message { get; private set; }

    public string Locale { get; private set; } = Locales.Default;

    public DateTimeOffset? ConsentAt { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Lead Create(
        string name,
        string phone,
        string? email,
        LeadInterest interest,
        LeadSource source,
        Guid? propertyId,
        string? message,
        string locale,
        DateTimeOffset? consentAt) => new()
        {
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            Interest = interest,
            Source = source,
            PropertyId = propertyId,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            Locale = Locales.IsSupported(locale) ? locale : Locales.Default,
            ConsentAt = consentAt,
        };

    public void Update(string name, string phone, string? email, LeadInterest interest, Guid? propertyId, string? notes)
    {
        Name = name.Trim();
        Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Interest = interest;
        PropertyId = propertyId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void ChangeStatus(LeadStatus status) => Status = status;

    public void MarkVisitScheduled()
    {
        if (Status is LeadStatus.New or LeadStatus.Contacted)
        {
            Status = LeadStatus.VisitScheduled;
        }
    }
}
