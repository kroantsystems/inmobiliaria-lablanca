using LaBlanca.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LaBlanca.Api.Configuration;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapApiHealthChecks(this IEndpointRouteBuilder app)
    {
        Map(app, "/health", _ => true);
        Map(app, "/health/live", _ => false);
        Map(app, "/health/ready", check => check.Tags.Contains(DependencyInjection.ReadyTag));
        return app;
    }

    private static void Map(IEndpointRouteBuilder app, string pattern, Func<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration, bool> predicate) =>
        app.MapHealthChecks(pattern, new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = async (context, report) =>
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
                });
            },
        })
        .AllowAnonymous()
        .DisableRateLimiting();
}
