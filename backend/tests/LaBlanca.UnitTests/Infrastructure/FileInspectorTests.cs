using System.IO.Compression;
using LaBlanca.Application.Abstractions.Files;
using LaBlanca.Domain.Media;
using LaBlanca.Infrastructure.Files;
using LaBlanca.Shared.Localization;
using Microsoft.Extensions.Options;

namespace LaBlanca.UnitTests.Infrastructure;

public class FileInspectorTests
{
    private const long MB = 1024 * 1024;

    private readonly FileInspector _inspector = new(Options.Create(new UploadOptions()));

    public static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    public static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 0];

    public static byte[] Webp => [.. "RIFF"u8.ToArray(), 0, 0, 0, 0, .. "WEBP"u8.ToArray()];

    public static byte[] Pdf => [.. "%PDF-1.7\n"u8.ToArray(), 0, 0, 0];

    public static byte[] Doc => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0, 0, 0, 0];

    public static byte[] Mp4 => [0, 0, 0, 0x18, .. "ftypmp42"u8.ToArray()];

    public static byte[] Webm => [0x1A, 0x45, 0xDF, 0xA3, 0, 0, 0, 0, 0, 0, 0, 0];

    public static byte[] Docx(bool word = true)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(zip.CreateEntry(word ? "word/document.xml" : "xl/workbook.xml").Open());
            writer.Write("<xml/>");
        }

        return buffer.ToArray();
    }

    public static TheoryData<string, string, byte[], MediaKind> ValidFiles => new()
    {
        { "foto.png", "image/png", Png, MediaKind.Image },
        { "foto.JPG", "image/jpeg", Jpeg, MediaKind.Image },
        { "foto.jpeg", "image/jpeg", Jpeg, MediaKind.Image },
        { "foto.webp", "image/webp", Webp, MediaKind.Image },
        { "tour.mp4", "video/mp4", Mp4, MediaKind.Video },
        { "tour.mov", "video/quicktime", Mp4, MediaKind.Video },
        { "tour.webm", "video/webm", Webm, MediaKind.Video },
        { "contrato.pdf", "application/pdf", Pdf, MediaKind.Document },
        { "modelo.doc", "application/msword", Doc, MediaKind.Document },
        { "modelo.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Docx(), MediaKind.Document },
    };

    [Theory]
    [MemberData(nameof(ValidFiles))]
    public void Accepts_allowed_types_with_matching_content(string name, string contentType, byte[] content, MediaKind kind)
    {
        var result = Inspect(name, contentType, content);

        result.IsValid.Should().BeTrue(result.ErrorKey);
        result.Kind.Should().Be(kind);
        result.ContentType.Should().Be(contentType);
    }

    [Theory]
    [InlineData("setup.exe", "application/octet-stream")]
    [InlineData("logo.svg", "image/svg+xml")]
    [InlineData("pagina.html", "text/html")]
    [InlineData("fotos.zip", "application/zip")]
    [InlineData("sem-extensao", "image/png")]
    public void Rejects_extensions_outside_the_allow_list(string name, string contentType)
    {
        Inspect(name, contentType, Png).ErrorKey.Should().Be(MessageKeys.FileTypeNotAllowed);
    }

    [Fact]
    public void Rejects_content_that_does_not_match_extension()
    {
        Inspect("foto.jpg", "image/jpeg", Pdf).ErrorKey.Should().Be(MessageKeys.FileContentMismatch);
        Inspect("contrato.pdf", "application/pdf", Png).ErrorKey.Should().Be(MessageKeys.FileContentMismatch);
        Inspect("planilha.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Docx(word: false))
            .ErrorKey.Should().Be(MessageKeys.FileContentMismatch);
    }

    [Fact]
    public void Rejects_declared_content_type_from_another_family()
    {
        Inspect("foto.png", "application/pdf", Png).ErrorKey.Should().Be(MessageKeys.FileContentMismatch);
    }

    [Theory]
    [InlineData("foto.png", 51 * MB, 50)]
    [InlineData("tour.mp4", 51 * MB, 50)]
    [InlineData("contrato.pdf", 20 * MB, 15)]
    public void Rejects_files_above_the_limit_for_their_type(string name, long length, int limit)
    {
        var content = name.EndsWith(".png") ? Png : name.EndsWith(".mp4") ? Mp4 : Pdf;

        var result = _inspector.Inspect(name, null, length, new MemoryStream(content));

        result.ErrorKey.Should().Be(MessageKeys.FileTooLarge);
        result.ErrorArgs.Should().Equal(limit);
    }

    [Fact]
    public void Accepts_image_exactly_at_50_mb()
    {
        _inspector.Inspect("foto.png", "image/png", 50 * MB, new MemoryStream(Png)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Inspection_leaves_stream_at_the_beginning()
    {
        var stream = new MemoryStream(Png);

        _inspector.Inspect("foto.png", "image/png", Png.Length, stream);

        stream.Position.Should().Be(0);
    }

    private FileInspectionResult Inspect(string name, string contentType, byte[] content) =>
        _inspector.Inspect(name, contentType, content.Length, new MemoryStream(content));
}
