using System.Net.Http.Json;
using System.Threading.Channels;
using LaBlanca.Application.Abstractions.Revalidation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LaBlanca.Infrastructure.Revalidation;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string? RevalidateUrl { get; set; }

    public string? RevalidateSecret { get; set; }
}

/// <summary>Fila em memória + envio em segundo plano para o route handler <c>/revalidate</c> do Next.js.</summary>
internal sealed class SiteRevalidationDispatcher(
    IHttpClientFactory httpClients,
    IOptions<SiteOptions> options,
    ILogger<SiteRevalidationDispatcher> logger) : BackgroundService, IRevalidationDispatcher
{
    public const string HttpClientName = "site-revalidation";

    private readonly Channel<IReadOnlyCollection<string>> _queue = Channel.CreateBounded<IReadOnlyCollection<string>>(
        new BoundedChannelOptions(200) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    internal IReadOnlyList<TimeSpan> RetryDelays { get; init; } = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)];

    public void Dispatch(IReadOnlyCollection<string> tags) => _queue.Writer.TryWrite(tags);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var tags in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await SendAsync(tags, stoppingToken);
        }
    }

    internal async Task SendAsync(IReadOnlyCollection<string> tags, CancellationToken cancellationToken)
    {
        var url = options.Value.RevalidateUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogDebug("Site revalidation skipped: Site:RevalidateUrl is not configured");
            return;
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(new { tags }) };
                request.Headers.Add("X-Revalidate-Secret", options.Value.RevalidateSecret ?? string.Empty);
                using var response = await httpClients.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                if (attempt >= RetryDelays.Count)
                {
                    logger.LogWarning(ex, "Site revalidation failed for tags {Tags}", string.Join(',', tags));
                    return;
                }

                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
        }
    }
}
