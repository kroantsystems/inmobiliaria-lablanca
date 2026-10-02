using System.Diagnostics;
using LaBlanca.Application.Abstractions.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LaBlanca.Application.Behaviors;

/// <summary>Registra só o nome e a duração da operação; o conteúdo nunca é logado (pode ter senhas e tokens).</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var kind = request is IBaseCommand ? "command" : "query";
        var name = typeof(TRequest).Name;
        var started = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        logger.LogInformation("Executed {RequestKind} {RequestName} in {ElapsedMs:0} ms", kind, name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return response;
    }
}
