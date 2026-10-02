using LaBlanca.Domain.Media;

namespace LaBlanca.Application.Abstractions.Files;

public sealed record FileInspectionResult(MediaKind Kind, string ContentType, string Extension, string? ErrorKey, object?[] ErrorArgs)
{
    public bool IsValid => ErrorKey is null;

    public static FileInspectionResult Invalid(string errorKey, params object?[] args) => new(default, string.Empty, string.Empty, errorKey, args);
}

/// <summary>Confere extensão, Content-Type declarado, tamanho e assinatura binária. Devolve o stream na posição inicial.</summary>
public interface IFileInspector
{
    FileInspectionResult Inspect(string fileName, string? declaredContentType, long length, Stream content);
}

public interface IFileStorage
{
    /// <summary>Grava com nome gerado e devolve a chave relativa; o nome original nunca vira caminho.</summary>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

public sealed record FileContent(Stream Stream, string ContentType, string FileName);
