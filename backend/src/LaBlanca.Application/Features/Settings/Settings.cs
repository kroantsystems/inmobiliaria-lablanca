using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Common;
using LaBlanca.Application.Features.Zones;
using LaBlanca.Domain.Settings;
using LaBlanca.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Settings;

public sealed record AdminSettingsDto(
    string CompanyName,
    string? Phone,
    string? WhatsappNumber,
    string? Email,
    string? Address,
    double? OfficeLatitude,
    double? OfficeLongitude,
    string? OpeningHours,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TiktokUrl,
    string? YoutubeUrl,
    decimal PygPerUsd,
    decimal BrlPerUsd,
    DateTimeOffset RatesUpdatedAt,
    decimal SimulatorAnnualRate,
    int MonthlySalesGoal,
    DateTimeOffset? UpdatedAt);

/// <summary>Só o que o site precisa: sem metas comerciais.</summary>
public sealed record PublicSettingsDto(
    string CompanyName,
    string? Phone,
    string? WhatsappNumber,
    string? Email,
    string? Address,
    double? OfficeLatitude,
    double? OfficeLongitude,
    string? OpeningHours,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TiktokUrl,
    string? YoutubeUrl,
    decimal PygPerUsd,
    decimal BrlPerUsd,
    DateTimeOffset RatesUpdatedAt,
    decimal SimulatorAnnualRate,
    IReadOnlyList<PublicZoneDto> Zones);

internal static class SettingsMapping
{
    public static AdminSettingsDto ToAdminDto(this SiteSettings s) => new(
        s.CompanyName, s.Phone, s.WhatsappNumber, s.Email, s.Address, s.OfficeLatitude, s.OfficeLongitude, s.OpeningHours,
        s.FacebookUrl, s.InstagramUrl, s.TiktokUrl, s.YoutubeUrl, s.PygPerUsd, s.BrlPerUsd, s.RatesUpdatedAt,
        s.SimulatorAnnualRate, s.MonthlySalesGoal, s.UpdatedAt);

    public static PublicSettingsDto ToPublicDto(this SiteSettings s, IReadOnlyList<PublicZoneDto> zones) => new(
        s.CompanyName, s.Phone, s.WhatsappNumber, s.Email, s.Address, s.OfficeLatitude, s.OfficeLongitude, s.OpeningHours,
        s.FacebookUrl, s.InstagramUrl, s.TiktokUrl, s.YoutubeUrl, s.PygPerUsd, s.BrlPerUsd, s.RatesUpdatedAt,
        s.SimulatorAnnualRate, zones);
}

public sealed record GetAdminSettingsQuery : IQuery<AdminSettingsDto>;

public sealed class GetAdminSettingsQueryHandler(IAppDbContext db) : IRequestHandler<GetAdminSettingsQuery, AdminSettingsDto>
{
    public async Task<AdminSettingsDto> Handle(GetAdminSettingsQuery request, CancellationToken cancellationToken) =>
        (await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException()).ToAdminDto();
}

public sealed record GetPublicSettingsQuery(string? Locale) : IQuery<PublicSettingsDto>;

public sealed class GetPublicSettingsQueryHandler(IAppDbContext db, ISender sender) : IRequestHandler<GetPublicSettingsQuery, PublicSettingsDto>
{
    public async Task<PublicSettingsDto> Handle(GetPublicSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException();
        var zones = await sender.Send(new GetPublicZonesQuery(request.Locale), cancellationToken);
        return settings.ToPublicDto(zones);
    }
}

public sealed record UpdateSettingsCommand(
    string CompanyName,
    string? Phone,
    string? WhatsappNumber,
    string? Email,
    string? Address,
    double? OfficeLatitude,
    double? OfficeLongitude,
    string? OpeningHours,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TiktokUrl,
    string? YoutubeUrl,
    decimal PygPerUsd,
    decimal BrlPerUsd,
    decimal SimulatorAnnualRate,
    int MonthlySalesGoal) : ICommand<AdminSettingsDto>;

public sealed class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.WhatsappNumber).InternationalPhone().When(x => !string.IsNullOrEmpty(x.WhatsappNumber));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.OpeningHours).MaximumLength(200);
        RuleFor(x => x.OfficeLatitude).Latitude();
        RuleFor(x => x.OfficeLongitude).Longitude();
        RuleFor(x => x.FacebookUrl).AbsoluteHttpUrl().MaximumLength(300).When(x => !string.IsNullOrEmpty(x.FacebookUrl));
        RuleFor(x => x.InstagramUrl).AbsoluteHttpUrl().MaximumLength(300).When(x => !string.IsNullOrEmpty(x.InstagramUrl));
        RuleFor(x => x.TiktokUrl).AbsoluteHttpUrl().MaximumLength(300).When(x => !string.IsNullOrEmpty(x.TiktokUrl));
        RuleFor(x => x.YoutubeUrl).AbsoluteHttpUrl().MaximumLength(300).When(x => !string.IsNullOrEmpty(x.YoutubeUrl));
        RuleFor(x => x.PygPerUsd).GreaterThan(0);
        RuleFor(x => x.BrlPerUsd).GreaterThan(0);
        RuleFor(x => x.SimulatorAnnualRate).InclusiveBetween(0, 100);
        RuleFor(x => x.MonthlySalesGoal).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSettingsCommandHandler(IAppDbContext db, ITenantContext tenant, TimeProvider clock)
    : IRequestHandler<UpdateSettingsCommand, AdminSettingsDto>
{
    public async Task<AdminSettingsDto> Handle(UpdateSettingsCommand c, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var settings = await db.SiteSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = SiteSettings.CreateDefault(tenant.TenantId, c.CompanyName, now);
            db.SiteSettings.Add(settings);
        }

        settings.UpdateContact(new SiteContact(c.CompanyName, c.Phone, c.WhatsappNumber, c.Email, c.Address, c.OfficeLatitude,
            c.OfficeLongitude, c.OpeningHours, c.FacebookUrl, c.InstagramUrl, c.TiktokUrl, c.YoutubeUrl));
        settings.UpdateRates(c.PygPerUsd, c.BrlPerUsd, now);
        settings.UpdateBusiness(c.SimulatorAnnualRate, c.MonthlySalesGoal);
        return settings.ToAdminDto();
    }
}
