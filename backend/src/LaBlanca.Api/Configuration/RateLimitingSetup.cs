using System.Globalization;
using System.Threading.RateLimiting;
using LaBlanca.Shared.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LaBlanca.Api.Configuration;

public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string PublicForms = "public-forms";
    public const string Analytics = "analytics";
}

public sealed class RateLimitRule
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}

public sealed class RateLimitingSettings
{
    public RateLimitRule Login { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };

    public RateLimitRule PublicForms { get; set; } = new() { PermitLimit = 10, WindowSeconds = 3600 };

    public RateLimitRule Analytics { get; set; } = new() { PermitLimit = 120, WindowSeconds = 60 };
}

public static class RateLimitingSetup
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteProblemAsync;

            AddPerIpFixedWindow(options, RateLimitPolicies.Login, settings.Login);
            AddPerIpFixedWindow(options, RateLimitPolicies.PublicForms, settings.PublicForms);
            AddPerIpFixedWindow(options, RateLimitPolicies.Analytics, settings.Analytics);
        });

        return services;
    }

    private static void AddPerIpFixedWindow(RateLimiterOptions options, string policy, RateLimitRule rule) =>
        options.AddPolicy(policy, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rule.PermitLimit,
                Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                QueueLimit = 0,
            }));

    private static async ValueTask WriteProblemAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Detail = AppMessages.Get(MessageKeys.TooManyRequests),
            },
        });
    }
}
