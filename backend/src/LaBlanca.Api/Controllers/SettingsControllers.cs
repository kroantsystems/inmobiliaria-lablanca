using LaBlanca.Application.Authorization;
using LaBlanca.Application.Features.Settings;
using LaBlanca.Application.Features.Zones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaBlanca.Api.Controllers;

[ApiController]
[Route("api/admin/settings")]
public sealed class AdminSettingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.SettingsRead)]
    public async Task<AdminSettingsDto> Get(CancellationToken cancellationToken) =>
        await sender.Send(new GetAdminSettingsQuery(), cancellationToken);

    [HttpPut]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public async Task<AdminSettingsDto> Update(UpdateSettingsCommand command, CancellationToken cancellationToken) =>
        await sender.Send(command, cancellationToken);
}

public sealed record ZoneRequest(string? Slug, string City, int SortOrder, IReadOnlyList<ZoneTranslationDto> Translations);

[ApiController]
[Route("api/admin/zones")]
public sealed class AdminZonesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.PropertiesRead)]
    public async Task<IReadOnlyList<AdminZoneDto>> List(CancellationToken cancellationToken) =>
        await sender.Send(new GetAdminZonesQuery(), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public async Task<ActionResult<AdminZoneDto>> Create(ZoneRequest request, CancellationToken cancellationToken)
    {
        var zone = await sender.Send(new SaveZoneCommand(null, request.Slug, request.City, request.SortOrder, request.Translations), cancellationToken);
        return Created($"/api/admin/zones/{zone.Id}", zone);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public async Task<AdminZoneDto> Update(Guid id, ZoneRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new SaveZoneCommand(id, request.Slug, request.City, request.SortOrder, request.Translations), cancellationToken);

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.SettingsWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteZoneCommand(id), cancellationToken);
        return NoContent();
    }
}

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicSettingsController(ISender sender) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<PublicSettingsDto> Settings([FromQuery] string? locale, CancellationToken cancellationToken) =>
        await sender.Send(new GetPublicSettingsQuery(locale), cancellationToken);

    [HttpGet("zones")]
    public async Task<IReadOnlyList<PublicZoneDto>> Zones([FromQuery] string? locale, CancellationToken cancellationToken) =>
        await sender.Send(new GetPublicZonesQuery(locale), cancellationToken);

    [HttpGet("zones/{slug}")]
    public async Task<PublicZoneDto> Zone(string slug, [FromQuery] string? locale, CancellationToken cancellationToken) =>
        await sender.Send(new GetPublicZoneQuery(slug, locale), cancellationToken);
}
