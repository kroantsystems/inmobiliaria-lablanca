using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LaBlanca.Application.Authorization;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class FilesTests(ApiFactory factory) : IAsyncLifetime
{
    private const int MB = 1024 * 1024;
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] PdfHeader = "%PDF-1.7\n"u8.ToArray();

    private HttpClient Admin => factory.CreateClientWithToken(Permissions.All.ToArray());

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static byte[] FileOf(byte[] header, int size)
    {
        var bytes = new byte[size];
        header.CopyTo(bytes, 0);
        return bytes;
    }

    private async Task<HttpResponseMessage> UploadAsync(byte[] content, string name, string contentType, Guid? propertyId = null, bool isPublic = true, HttpClient? client = null)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", name);
        if (propertyId is { } id)
        {
            form.Add(new StringContent(id.ToString()), "propertyId");
        }

        form.Add(new StringContent(isPublic ? "true" : "false"), "isPublic");
        form.Add(new StringContent("Fachada principal"), "description");
        return await (client ?? Admin).PostAsync("/api/admin/files", form);
    }

    private async Task<Guid> CreatePropertyAsync(string title = "Casa con galería")
    {
        var response = await Admin.PostAsJsonAsync("/api/admin/properties", new
        {
            operation = "Sale",
            type = "House",
            price = 100_000m,
            currency = "USD",
            zoneId = Guid.Parse("a1d0f3c2-1b4e-4c6a-9f20-0a1b2c3d4e05"),
            city = "Hernandarias",
            translations = new[] { new { locale = "es", title } },
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> IdOf(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task PublishAsync(Guid propertyId)
    {
        (await Admin.PutAsJsonAsync($"/api/admin/properties/{propertyId}/status", new { status = "Available" })).EnsureSuccessStatusCode();
        (await Admin.PostAsync($"/api/admin/properties/{propertyId}/publish", null)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Image_of_about_50_mb_is_uploaded_linked_and_publishes_the_property()
    {
        var propertyId = await CreatePropertyAsync();

        var mediaId = await IdOf(await UploadAsync(FileOf(PngHeader, 50 * MB - 1024), "fachada.png", "image/png", propertyId));
        await PublishAsync(propertyId);

        var detail = await Admin.GetFromJsonAsync<JsonElement>($"/api/admin/properties/{propertyId}");
        var media = detail.GetProperty("media")[0];
        media.GetProperty("id").GetGuid().Should().Be(mediaId);
        media.GetProperty("kind").GetString().Should().Be("Image");
        media.GetProperty("sizeBytes").GetInt64().Should().Be(50 * MB - 1024);

        var publicResponse = await factory.CreateClient().GetAsync($"/api/public/media/{mediaId}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        publicResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        publicResponse.Headers.CacheControl!.ToString().Should().Contain("immutable").And.Contain("max-age=31536000");
        publicResponse.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
    }

    [Fact]
    public async Task Files_above_type_limits_are_rejected_with_the_limit_in_the_message()
    {
        var image = await UploadAsync(FileOf(PngHeader, 51 * MB), "grande.png", "image/png");
        image.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await image.Content.ReadAsStringAsync()).Should().Contain("50 MB");

        var pdf = await UploadAsync(FileOf(PdfHeader, 20 * MB), "contrato.pdf", "application/pdf");
        pdf.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await pdf.Content.ReadAsStringAsync()).Should().Contain("15 MB");
    }

    [Theory]
    [InlineData("setup.exe", "application/octet-stream")]
    [InlineData("logo.svg", "image/svg+xml")]
    [InlineData("pagina.html", "text/html")]
    public async Task Forbidden_extensions_are_rejected(string name, string contentType)
    {
        (await UploadAsync(FileOf(PngHeader, 1024), name, contentType)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Disguised_file_is_rejected()
    {
        var response = await UploadAsync(FileOf(PdfHeader, 1024), "foto.jpg", "image/jpeg");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("file")[0].GetString()
            .Should().Contain("não corresponde");
    }

    [Fact]
    public async Task Malicious_name_is_kept_only_as_metadata_and_file_is_stored_with_guid()
    {
        var id = await IdOf(await UploadAsync(FileOf(PngHeader, 2048), "../../appsettings.json.png", "image/png"));

        using var scope = factory.Services.CreateScope();
        var media = await scope.ServiceProvider.GetRequiredService<AppDbContext>().MediaFiles.SingleAsync(m => m.Id == id);
        media.OriginalName.Should().Be("appsettings.json.png");
        media.StorageKey.Should().MatchRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}\.png$");
        File.Exists(Path.Combine(factory.StorageRoot, media.StorageKey)).Should().BeTrue();
    }

    [Fact]
    public async Task Documents_are_private_and_only_downloadable_by_admin()
    {
        var propertyId = await CreatePropertyAsync();
        await IdOf(await UploadAsync(FileOf(PngHeader, 1024), "foto.png", "image/png", propertyId));
        await PublishAsync(propertyId);
        var docId = await IdOf(await UploadAsync(FileOf(PdfHeader, 1024), "Contrato Silva.pdf", "application/pdf", propertyId, isPublic: true));

        var files = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/files");
        var doc = files.GetProperty("items").EnumerateArray().Single(f => f.GetProperty("id").GetGuid() == docId);
        doc.GetProperty("isPublic").GetBoolean().Should().BeFalse();
        doc.GetProperty("propertyTitle").GetString().Should().Be("Casa con galería");

        (await factory.CreateClient().GetAsync($"/api/public/media/{docId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await factory.CreateClient().GetAsync($"/api/admin/files/{docId}/content")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var download = await Admin.GetAsync($"/api/admin/files/{docId}/content");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        download.Content.Headers.ContentDisposition.FileNameStar.Should().Be("Contrato Silva.pdf");
    }

    [Fact]
    public async Task Unlinked_document_stays_in_the_library()
    {
        var id = await IdOf(await UploadAsync(FileOf(PdfHeader, 1024), "modelo.pdf", "application/pdf"));

        var files = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/files");
        var file = files.GetProperty("items").EnumerateArray().Single(f => f.GetProperty("id").GetGuid() == id);
        file.GetProperty("propertyId").ValueKind.Should().Be(JsonValueKind.Null);
        file.GetProperty("description").GetString().Should().Be("Fachada principal");
    }

    [Fact]
    public async Task Cover_and_order_define_the_public_gallery()
    {
        var propertyId = await CreatePropertyAsync();
        var a = await IdOf(await UploadAsync(FileOf(PngHeader, 1024), "a.png", "image/png", propertyId));
        var b = await IdOf(await UploadAsync(FileOf(PngHeader, 1024), "b.png", "image/png", propertyId));
        var c = await IdOf(await UploadAsync(FileOf(PngHeader, 1024), "c.png", "image/png", propertyId));
        await PublishAsync(propertyId);

        (await Admin.PutAsJsonAsync($"/api/admin/properties/{propertyId}/media/order", new { mediaIds = new[] { c, a, b } })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Admin.PutAsync($"/api/admin/properties/{propertyId}/media/{b}/cover", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/properties/es/casa-con-galeria");
        detail.GetProperty("gallery").EnumerateArray().Select(g => g.GetProperty("url").GetString())
            .Should().Equal($"/api/public/media/{b}", $"/api/public/media/{c}", $"/api/public/media/{a}");
    }

    [Fact]
    public async Task Admin_list_thumbnail_uses_the_public_url_of_a_public_image()
    {
        var propertyId = await CreatePropertyAsync();
        await IdOf(await UploadAsync(FileOf(PngHeader, 1024), "privada.png", "image/png", propertyId, isPublic: false));
        var visible = await IdOf(await UploadAsync(FileOf(PngHeader, 1024), "fachada.png", "image/png", propertyId));

        var list = await Admin.GetFromJsonAsync<JsonElement>("/api/admin/properties");

        list.GetProperty("items")[0].GetProperty("coverUrl").GetString().Should().Be($"/api/public/media/{visible}");
    }

    [Fact]
    public async Task Last_public_image_of_published_property_cannot_be_deleted()
    {
        var propertyId = await CreatePropertyAsync();
        var image = await IdOf(await UploadAsync(FileOf(PngHeader, 1024), "unica.png", "image/png", propertyId));
        await PublishAsync(propertyId);

        var response = await Admin.DeleteAsync($"/api/admin/files/{image}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("ao menos uma imagem pública");
    }

    [Fact]
    public async Task Deleting_a_file_removes_record_and_stored_content()
    {
        var id = await IdOf(await UploadAsync(FileOf(PdfHeader, 1024), "apagar.pdf", "application/pdf"));
        string key;
        using (var scope = factory.Services.CreateScope())
        {
            key = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().MediaFiles.SingleAsync(m => m.Id == id)).StorageKey;
        }

        (await Admin.DeleteAsync($"/api/admin/files/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        File.Exists(Path.Combine(factory.StorageRoot, key)).Should().BeFalse();
        (await Admin.GetAsync($"/api/admin/files/{id}/content")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
