using LaBlanca.Shared.Localization;

namespace LaBlanca.Shared.Errors;

/// <summary>Exceção de aplicação com status HTTP e chave de mensagem localizável.</summary>
public abstract class AppException(int statusCode, string messageKey, object?[] args) : Exception(messageKey)
{
    public int StatusCode { get; } = statusCode;

    public string MessageKey { get; } = messageKey;

    public object?[] MessageArgs { get; } = args;
}

public sealed class NotFoundException(string messageKey = MessageKeys.NotFound, params object?[] args)
    : AppException(404, messageKey, args);

public sealed class ConflictException(string messageKey, params object?[] args)
    : AppException(409, messageKey, args);

public sealed class BusinessRuleException(string messageKey, params object?[] args)
    : AppException(400, messageKey, args);

public sealed class UnauthorizedException(string messageKey = MessageKeys.Unauthorized, params object?[] args)
    : AppException(401, messageKey, args);

public sealed class ForbiddenException(string messageKey = MessageKeys.Forbidden, params object?[] args)
    : AppException(403, messageKey, args);

public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : AppException(400, MessageKeys.Validation, [])
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
