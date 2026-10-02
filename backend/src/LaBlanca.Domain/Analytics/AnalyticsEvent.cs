using LaBlanca.Domain.Common;

namespace LaBlanca.Domain.Analytics;

public enum AnalyticsEventType
{
    PageView,
    PropertyView,
    WhatsAppClick,
    ContactClick,
}

/// <summary>Evento anônimo: sem IP, user agent, cookies ou dados pessoais.</summary>
public sealed class AnalyticsEvent : TenantEntity
{
    private AnalyticsEvent()
    {
    }

    public AnalyticsEventType Type { get; private set; }

    public Guid? PropertyId { get; private set; }

    public string Path { get; private set; } = string.Empty;

    public string Locale { get; private set; } = Locales.Default;

    public Guid SessionId { get; private set; }

    public string? ReferrerHost { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static AnalyticsEvent Record(
        AnalyticsEventType type,
        Guid? propertyId,
        string path,
        string locale,
        Guid sessionId,
        string? referrerHost,
        DateTimeOffset occurredAt) => new()
        {
            Type = type,
            PropertyId = propertyId,
            Path = path,
            Locale = Locales.IsSupported(locale) ? locale : Locales.Default,
            SessionId = sessionId,
            ReferrerHost = string.IsNullOrWhiteSpace(referrerHost) ? null : referrerHost.Trim().ToLowerInvariant(),
            OccurredAt = occurredAt,
        };
}
