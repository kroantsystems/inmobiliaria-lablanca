using LaBlanca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LaBlanca.Infrastructure.Authentication;

/// <summary>Remove diariamente refresh tokens expirados de todos os tenants.</summary>
internal sealed class RefreshTokenCleanupService(IServiceScopeFactory scopes, TimeProvider clock, ILogger<RefreshTokenCleanupService> logger)
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
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = clock.GetUtcNow();
                var removed = await db.RefreshTokens.IgnoreQueryFilters()
                    .Where(t => t.ExpiresAt < now)
                    .ExecuteDeleteAsync(stoppingToken);
                logger.LogInformation("Removed {Count} expired refresh tokens", removed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Refresh token cleanup failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
