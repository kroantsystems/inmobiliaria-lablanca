using LaBlanca.Application.Authorization;
using LaBlanca.Application.Features.Properties;
using LaBlanca.Domain.Properties;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaBlanca.Api.Controllers;

public sealed record PropertyStatusRequest(PropertyStatus Status);

public sealed record PropertyFeaturedRequest(bool Featured);

[ApiController]
[Route("api/admin/properties")]
public sealed class AdminPropertiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.PropertiesRead)]
    public async Task<PagedResult<AdminPropertyListItem>> List([FromQuery] int page = 1, [FromQuery] int pageSize = GetAdminPropertiesQuery.MaxPageSize, CancellationToken cancellationToken = default) =>
        await sender.Send(new GetAdminPropertiesQuery(page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PropertiesRead)]
    public async Task<AdminPropertyDto> Get(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new GetAdminPropertyQuery(id), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<ActionResult<AdminPropertyDto>> Create(PropertyInput input, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new SavePropertyCommand(null, input), cancellationToken);
        return Created($"/api/admin/properties/{id}", await sender.Send(new GetAdminPropertyQuery(id), cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<AdminPropertyDto> Update(Guid id, PropertyInput input, CancellationToken cancellationToken)
    {
        await sender.Send(new SavePropertyCommand(id, input), cancellationToken);
        return await sender.Send(new GetAdminPropertyQuery(id), cancellationToken);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public Task<AdminPropertyDto> Publish(Guid id, CancellationToken cancellationToken) => Change(id, PropertyAction.Publish, cancellationToken);

    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public Task<AdminPropertyDto> Unpublish(Guid id, CancellationToken cancellationToken) => Change(id, PropertyAction.Unpublish, cancellationToken);

    [HttpPut("{id:guid}/featured")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public Task<AdminPropertyDto> Featured(Guid id, PropertyFeaturedRequest request, CancellationToken cancellationToken) =>
        Change(id, request.Featured ? PropertyAction.Feature : PropertyAction.Unfeature, cancellationToken);

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<AdminPropertyDto> Status(Guid id, PropertyStatusRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePropertyStatusCommand(id, request.Status), cancellationToken);
        return await sender.Send(new GetAdminPropertyQuery(id), cancellationToken);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<AdminPropertyDto> Archive(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePropertyStatusCommand(id, PropertyStatus.Archived), cancellationToken);
        return await sender.Send(new GetAdminPropertyQuery(id), cancellationToken);
    }

    private async Task<AdminPropertyDto> Change(Guid id, PropertyAction action, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePropertyPublicationCommand(id, action), cancellationToken);
        return await sender.Send(new GetAdminPropertyQuery(id), cancellationToken);
    }
}

[ApiController]
[AllowAnonymous]
[Route("api/public/properties")]
public sealed class PublicPropertiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<PublicPropertyCard>> Search([FromQuery] SearchPublicPropertiesQuery query, CancellationToken cancellationToken) =>
        await sender.Send(query, cancellationToken);

    [HttpGet("featured")]
    public async Task<IReadOnlyList<PublicPropertyCard>> Featured([FromQuery] string? locale, [FromQuery] int take = 6, CancellationToken cancellationToken = default) =>
        await sender.Send(new GetFeaturedPropertiesQuery(locale, take), cancellationToken);

    [HttpGet("map")]
    public async Task<IReadOnlyList<PublicPropertyCard>> Map([FromQuery] string? locale, CancellationToken cancellationToken) =>
        await sender.Send(new GetPublicMapQuery(locale), cancellationToken);

    [HttpGet("sitemap")]
    public async Task<IReadOnlyList<SitemapPropertyEntry>> Sitemap(CancellationToken cancellationToken) =>
        await sender.Send(new GetSitemapPropertiesQuery(), cancellationToken);

    [HttpGet("{id:guid}/similar")]
    public async Task<IReadOnlyList<PublicPropertyCard>> Similar(Guid id, [FromQuery] string? locale, [FromQuery] int take = 3, CancellationToken cancellationToken = default) =>
        await sender.Send(new GetSimilarPropertiesQuery(id, locale, take), cancellationToken);

    [HttpGet("{locale}/{slug}")]
    public async Task<PublicPropertyDetail> Get(string locale, string slug, CancellationToken cancellationToken) =>
        await sender.Send(new GetPublicPropertyQuery(locale, slug), cancellationToken);
}
