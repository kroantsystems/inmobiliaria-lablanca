using LaBlanca.Application.Abstractions.Messaging;
using MediatR;

namespace LaBlanca.Application.Abstractions.Revalidation;

/// <summary>Tags de cache usadas pelo Next.js; precisam bater com as do frontend.</summary>
public static class CacheTags
{
    public const string Properties = "properties";
    public const string Featured = "featured";
    public const string Sitemap = "sitemap";
    public const string Llms = "llms";
    public const string Settings = "settings";
    public const string Zones = "zones";

    public static string Property(Guid id) => $"property:{id}";

    public static string[] ForProperty(Guid id) => [Properties, Featured, Sitemap, Llms, Property(id)];
}

/// <summary>Acumula tags durante o Command; o envio só acontece depois do commit.</summary>
public interface IRevalidationNotifier
{
    void Request(params string[] tags);
}

/// <summary>Envia as tags ao site (fila em segundo plano); falhas não afetam a operação do admin.</summary>
public interface IRevalidationDispatcher
{
    void Dispatch(IReadOnlyCollection<string> tags);
}

internal sealed class RevalidationNotifier : IRevalidationNotifier
{
    private readonly HashSet<string> _pending = [];

    public void Request(params string[] tags) => _pending.UnionWith(tags);

    public IReadOnlyCollection<string> Drain()
    {
        var tags = _pending.ToArray();
        _pending.Clear();
        return tags;
    }
}

/// <summary>Fica fora da transação: só despacha se o Command (e o commit) terminaram sem erro.</summary>
internal sealed class RevalidationBehavior<TRequest, TResponse>(RevalidationNotifier notifier, IRevalidationDispatcher dispatcher)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        if (request is IBaseCommand)
        {
            var tags = notifier.Drain();
            if (tags.Count > 0)
            {
                dispatcher.Dispatch(tags);
            }
        }

        return response;
    }
}
