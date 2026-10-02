namespace LaBlanca.Domain.Common;

/// <summary>Violação de regra de negócio. A mensagem é uma chave de recurso traduzida pela API.</summary>
public class DomainException(string messageKey) : Exception(messageKey)
{
    public string MessageKey => Message;
}
