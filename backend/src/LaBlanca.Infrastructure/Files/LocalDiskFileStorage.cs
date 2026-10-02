using LaBlanca.Application.Abstractions.Files;
using LaBlanca.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace LaBlanca.Infrastructure.Files;

/// <summary>Arquivos em disco local fora da pasta pública, com chaves <c>yyyy/MM/{guid}{ext}</c>.</summary>
internal sealed class LocalDiskFileStorage(IOptions<StorageOptions> options, TimeProvider clock) : IFileStorage
{
    private string Root => Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        if (extension.Length > 10 || extension.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '.'))
        {
            throw new ArgumentException("Invalid extension.", nameof(extension));
        }

        var now = clock.GetUtcNow();
        var key = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
        return key;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new FileStream(Resolve(key), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        var root = Root;
        var path = Path.GetFullPath(Path.Combine(root, key));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Storage key resolves outside the storage root.");
        }

        return path;
    }
}
