using System.IO.Compression;
using LaBlanca.Application.Abstractions.Files;
using LaBlanca.Domain.Media;
using LaBlanca.Shared.Localization;
using Microsoft.Extensions.Options;

namespace LaBlanca.Infrastructure.Files;

public sealed class UploadOptions
{
    public const string SectionName = "Uploads";

    public int ImageMaxMegabytes { get; set; } = 50;

    public int VideoMaxMegabytes { get; set; } = 50;

    public int DocumentMaxMegabytes { get; set; } = 15;

    /// <summary>Limite do corpo da requisição de upload (arquivo + campos do formulário).</summary>
    public const long RequestLimitBytes = 55L * 1024 * 1024;
}

internal sealed class FileInspector(IOptions<UploadOptions> options) : IFileInspector
{
    private sealed record FileType(MediaKind Kind, string ContentType, string[] AcceptedContentTypes, Func<byte[], Stream, bool> Matches);

    private static readonly Dictionary<string, FileType> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = new(MediaKind.Image, "image/png", ["image/png"], (h, _) => StartsWith(h, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])),
        [".jpg"] = Jpeg(),
        [".jpeg"] = Jpeg(),
        [".webp"] = new(MediaKind.Image, "image/webp", ["image/webp"], (h, _) => StartsWith(h, "RIFF"u8) && StartsWith(h[8..], "WEBP"u8)),
        [".mp4"] = new(MediaKind.Video, "video/mp4", ["video/mp4"], (h, _) => IsIsoMedia(h)),
        [".mov"] = new(MediaKind.Video, "video/quicktime", ["video/quicktime", "video/mp4"], (h, _) => IsIsoMedia(h)),
        [".webm"] = new(MediaKind.Video, "video/webm", ["video/webm"], (h, _) => StartsWith(h, [0x1A, 0x45, 0xDF, 0xA3])),
        [".pdf"] = new(MediaKind.Document, "application/pdf", ["application/pdf"], (h, _) => StartsWith(h, "%PDF"u8)),
        [".doc"] = new(MediaKind.Document, "application/msword", ["application/msword"], (h, _) => StartsWith(h, [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1])),
        [".docx"] = new(
            MediaKind.Document,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            (h, s) => StartsWith(h, [0x50, 0x4B, 0x03, 0x04]) && HasWordDocument(s)),
    };

    public static IReadOnlyCollection<string> AllowedExtensions => Types.Keys;

    public FileInspectionResult Inspect(string fileName, string? declaredContentType, long length, Stream content)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!Types.TryGetValue(extension, out var type))
        {
            return FileInspectionResult.Invalid(MessageKeys.FileTypeNotAllowed, string.Join(", ", AllowedExtensions));
        }

        var limit = type.Kind switch
        {
            MediaKind.Image => options.Value.ImageMaxMegabytes,
            MediaKind.Video => options.Value.VideoMaxMegabytes,
            _ => options.Value.DocumentMaxMegabytes,
        };
        if (length > limit * 1024L * 1024L)
        {
            return FileInspectionResult.Invalid(MessageKeys.FileTooLarge, limit);
        }

        if (!string.IsNullOrWhiteSpace(declaredContentType)
            && declaredContentType != "application/octet-stream"
            && !type.AcceptedContentTypes.Contains(declaredContentType.Split(';')[0].Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return FileInspectionResult.Invalid(MessageKeys.FileContentMismatch);
        }

        var header = new byte[16];
        var read = content.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        content.Position = 0;
        var matches = read >= 4 && type.Matches(header[..read], content);
        content.Position = 0;

        return matches
            ? new FileInspectionResult(type.Kind, type.ContentType, extension, null, [])
            : FileInspectionResult.Invalid(MessageKeys.FileContentMismatch);
    }

    private static FileType Jpeg() =>
        new(MediaKind.Image, "image/jpeg", ["image/jpeg", "image/jpg", "image/pjpeg"], (h, _) => StartsWith(h, [0xFF, 0xD8, 0xFF]));

    private static bool StartsWith(ReadOnlySpan<byte> header, ReadOnlySpan<byte> signature) => header.StartsWith(signature);

    private static bool IsIsoMedia(byte[] header) => header.Length >= 8 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8);

    private static bool HasWordDocument(Stream content)
    {
        try
        {
            using var zip = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            return zip.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
