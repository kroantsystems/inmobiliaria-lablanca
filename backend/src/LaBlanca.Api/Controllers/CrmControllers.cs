using LaBlanca.Api.Configuration;
using LaBlanca.Application.Authorization;
using LaBlanca.Application.Features.Leads;
using LaBlanca.Application.Features.Owners;
using LaBlanca.Application.Features.Properties;
using LaBlanca.Application.Features.Visits;
using LaBlanca.Domain.Leads;
using LaBlanca.Domain.Visits;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LaBlanca.Api.Controllers;

public sealed record IdResponse(Guid Id);

public sealed record LeadRequest(string Name, string Phone, string? Email, LeadInterest Interest, Guid? PropertyId, string? Notes);

public sealed record LeadStatusRequest(LeadStatus Status);

public sealed record OwnerRequest(string Name, string Phone, string? Email, string? Document, string? Notes);

public sealed record VisitRequest(Guid PropertyId, Guid? LeadId, string? ClientName, DateTimeOffset StartsAt, int? DurationMinutes, string? Notes);

public sealed record VisitStatusRequest(VisitStatus Status);

[ApiController]
[AllowAnonymous]
[Route("api/public/leads")]
public sealed class PublicLeadsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.PublicForms)]
    public async Task<IActionResult> Submit(SubmitPublicLeadCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Accepted();
    }
}

[ApiController]
[Route("api/admin/leads")]
public sealed class AdminLeadsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.LeadsRead)]
    public async Task<PagedResult<LeadDto>> List([FromQuery] int page = 1, [FromQuery] int pageSize = GetLeadsQuery.MaxPageSize, CancellationToken cancellationToken = default) =>
        await sender.Send(new GetLeadsQuery(page, pageSize), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.LeadsWrite)]
    public async Task<ActionResult<IdResponse>> Create(LeadRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new SaveLeadCommand(null, request.Name, request.Phone, request.Email, request.Interest, request.PropertyId, request.Notes), cancellationToken);
        return Created($"/api/admin/leads/{id}", new IdResponse(id));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.LeadsWrite)]
    public async Task<IActionResult> Update(Guid id, LeadRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SaveLeadCommand(id, request.Name, request.Phone, request.Email, request.Interest, request.PropertyId, request.Notes), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Permissions.LeadsWrite)]
    public async Task<IActionResult> Status(Guid id, LeadStatusRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeLeadStatusCommand(id, request.Status), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.LeadsWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteLeadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/convert-to-owner")]
    [Authorize(Policy = Permissions.OwnersWrite)]
    public async Task<ActionResult<IdResponse>> ConvertToOwner(Guid id, CancellationToken cancellationToken)
    {
        var ownerId = await sender.Send(new ConvertLeadToOwnerCommand(id), cancellationToken);
        return Created($"/api/admin/owners/{ownerId}", new IdResponse(ownerId));
    }
}

[ApiController]
[Route("api/admin/owners")]
public sealed class AdminOwnersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.OwnersRead)]
    public async Task<PagedResult<OwnerDto>> List([FromQuery] int page = 1, [FromQuery] int pageSize = GetOwnersQuery.MaxPageSize, CancellationToken cancellationToken = default) =>
        await sender.Send(new GetOwnersQuery(page, pageSize), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.OwnersWrite)]
    public async Task<ActionResult<IdResponse>> Create(OwnerRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new SaveOwnerCommand(null, request.Name, request.Phone, request.Email, request.Document, request.Notes), cancellationToken);
        return Created($"/api/admin/owners/{id}", new IdResponse(id));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.OwnersWrite)]
    public async Task<IActionResult> Update(Guid id, OwnerRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SaveOwnerCommand(id, request.Name, request.Phone, request.Email, request.Document, request.Notes), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.OwnersWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteOwnerCommand(id), cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/admin/visits")]
public sealed class AdminVisitsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.VisitsRead)]
    public async Task<IReadOnlyList<VisitDto>> Range([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken) =>
        await sender.Send(new GetVisitsQuery(from, to), cancellationToken);

    [HttpGet("upcoming")]
    [Authorize(Policy = Permissions.VisitsRead)]
    public async Task<IReadOnlyList<VisitDto>> Upcoming([FromQuery] int take = 10, CancellationToken cancellationToken = default) =>
        await sender.Send(new GetUpcomingVisitsQuery(take), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.VisitsWrite)]
    public async Task<ActionResult<VisitDto>> Create(VisitRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new SaveVisitCommand(null, request.PropertyId, request.LeadId, request.ClientName, request.StartsAt, request.DurationMinutes, request.Notes), cancellationToken);
        return Created($"/api/admin/visits/{id}", await sender.Send(new GetVisitQuery(id), cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.VisitsWrite)]
    public async Task<VisitDto> Update(Guid id, VisitRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SaveVisitCommand(id, request.PropertyId, request.LeadId, request.ClientName, request.StartsAt, request.DurationMinutes, request.Notes), cancellationToken);
        return await sender.Send(new GetVisitQuery(id), cancellationToken);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Permissions.VisitsWrite)]
    public async Task<IActionResult> Status(Guid id, VisitStatusRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeVisitStatusCommand(id, request.Status), cancellationToken);
        return NoContent();
    }
}
