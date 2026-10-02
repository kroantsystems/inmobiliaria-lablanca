using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LaBlanca.Infrastructure.Storage;

internal sealed class StorageHealthCheck(IOptions<StorageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(options.Value.RootPath);
            var probe = Path.Combine(options.Value.RootPath, $".health-{Guid.NewGuid():N}");
            await File.WriteAllTextAsync(probe, "ok", cancellationToken);
            File.Delete(probe);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return HealthCheckResult.Unhealthy("Storage folder is not writable.", ex);
        }
    }
}
