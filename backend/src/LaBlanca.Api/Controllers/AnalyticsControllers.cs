using LaBlanca.Api.Configuration;
using LaBlanca.Application.Authorization;
using LaBlanca.Application.Features.Analytics;
using LaBlanca.Domain.Analytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace LaBlanca.Api.Controllers;

public sealed record AnalyticsEventRequest(AnalyticsEventType Type, Guid? PropertyId, string Path, string? Locale, Guid SessionId, string? Referrer);

[ApiController]
[AllowAnonymous]
[Route("api/public/analytics")]
public sealed class PublicAnalyticsController(ISender sender) : ControllerBase
{
    [HttpPost("events")]
    [EnableRateLimiting(RateLimitPolicies.Analytics)]
    public async Task<IActionResult> Record(AnalyticsEventRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new RecordEventCommand(request.Type, request.PropertyId, request.Path, request.Locale, request.SessionId, request.Referrer, Request.Headers[HeaderNames.UserAgent].ToString()),
            cancellationToken);
        return Accepted();
    }
}

[ApiController]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController(ISender sender) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = Permissions.DashboardRead)]
    public async Task<DashboardSummaryDto> Summary(CancellationToken cancellationToken) =>
        await sender.Send(new GetDashboardSummaryQuery(), cancellationToken);

    [HttpGet("traffic")]
    [Authorize(Policy = Permissions.DashboardRead)]
    public async Task<IReadOnlyList<TrafficPointDto>> Traffic([FromQuery] int days = 7, CancellationToken cancellationToken = default) =>
        await sender.Send(new GetTrafficQuery(days), cancellationToken);

    [HttpGet("top-properties")]
    [Authorize(Policy = Permissions.DashboardRead)]
    public async Task<IReadOnlyList<TopPropertyDto>> TopProperties(CancellationToken cancellationToken) =>
        await sender.Send(new GetTopPropertiesQuery(), cancellationToken);
}
