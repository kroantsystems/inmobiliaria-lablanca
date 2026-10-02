using System.Net;
using LaBlanca.Infrastructure.Revalidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace LaBlanca.UnitTests.Infrastructure;

public class SiteRevalidationDispatcherTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add((request, await request.Content!.ReadAsStringAsync(cancellationToken)));
            return respond(request);
        }
    }

    private static SiteRevalidationDispatcher Create(StubHandler handler, string? url = "http://site/revalidate")
    {
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, disposeHandler: false));
        return new SiteRevalidationDispatcher(
            clients.Object,
            Options.Create(new SiteOptions { RevalidateUrl = url, RevalidateSecret = "s3cr3t" }),
            NullLogger<SiteRevalidationDispatcher>.Instance)
        {
            RetryDelays = [TimeSpan.Zero, TimeSpan.Zero],
        };
    }

    [Fact]
    public async Task Posts_tags_with_secret_header()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await Create(handler).SendAsync(["properties", "sitemap"], CancellationToken.None);

        var (request, body) = handler.Calls.Should().ContainSingle().Subject;
        request.RequestUri!.ToString().Should().Be("http://site/revalidate");
        request.Headers.GetValues("X-Revalidate-Secret").Should().ContainSingle("s3cr3t");
        body.Should().Contain("properties").And.Contain("sitemap");
    }

    [Fact]
    public async Task Failure_is_retried_and_never_thrown()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var act = () => Create(handler).SendAsync(["properties"], CancellationToken.None);

        await act.Should().NotThrowAsync();
        handler.Calls.Should().HaveCount(3);
    }

    [Fact]
    public async Task Missing_url_skips_the_call()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await Create(handler, url: null).SendAsync(["properties"], CancellationToken.None);

        handler.Calls.Should().BeEmpty();
    }
}
