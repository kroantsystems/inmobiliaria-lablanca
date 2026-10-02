using LaBlanca.Application.Features.Analytics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LaBlanca.Infrastructure.Analytics;

internal sealed class AnalyticsRetentionService(IServiceScopeFactory scopes, TimeProvider clock, ILogger<AnalyticsRetentionService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24), clock);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var removed = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PurgeOldAnalyticsEventsCommand(), stoppingToken);
                logger.LogInformation("Removed {Count} analytics events past retention", removed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Analytics retention failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
