using LaBlanca.Shared.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LaBlanca.Shared.Errors;

public sealed record ProblemDescriptor(int Status, string Detail, IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>Mapeia exceções sem tipo conhecido pelo Shared (ex.: exceções de domínio) para status HTTP.</summary>
public sealed class ExceptionMappingOptions
{
    private readonly List<(Type Type, int Status)> _mappings = [];

    internal IReadOnlyList<(Type Type, int Status)> Mappings => _mappings;

    /// <summary>A mensagem da exceção mapeada é tratada como chave de recurso.</summary>
    public ExceptionMappingOptions Map<TException>(int statusCode) where TException : Exception
    {
        _mappings.Add((typeof(TException), statusCode));
        return this;
    }
}

public sealed class ExceptionProblemMapper(IOptions<ExceptionMappingOptions> options)
{
    public ProblemDescriptor Map(Exception exception)
    {
        switch (exception)
        {
            case RequestValidationException validation:
                return new(validation.StatusCode, AppMessages.Get(validation.MessageKey), validation.Errors);
            case AppException app:
                return new(app.StatusCode, AppMessages.Get(app.MessageKey, app.MessageArgs));
            case BadHttpRequestException badRequest:
                return new(badRequest.StatusCode, AppMessages.Get(MessageKeys.ProblemTitle(badRequest.StatusCode)));
        }

        foreach (var (type, status) in options.Value.Mappings)
        {
            if (type.IsInstanceOfType(exception))
            {
                return new(status, AppMessages.Get(exception.Message));
            }
        }

        return new(StatusCodes.Status500InternalServerError, AppMessages.Get(MessageKeys.Unexpected));
    }
}
