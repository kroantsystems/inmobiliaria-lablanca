using LaBlanca.Application.Authorization;
using LaBlanca.Application.Features.Files;
using LaBlanca.Application.Features.Properties;
using LaBlanca.Domain.Media;
using LaBlanca.Infrastructure.Files;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace LaBlanca.Api.Controllers;

public sealed record UploadedFileResponse(Guid Id);

public sealed record UpdateFileRequest(Guid? PropertyId, string? Description, string? AltText, bool IsPublic);

public sealed record ReorderMediaRequest(IReadOnlyList<Guid> MediaIds);

[ApiController]
[Route("api/admin/files")]
public sealed class AdminFilesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.FilesRead)]
    public async Task<PagedResult<FileListItem>> List([FromQuery] int page = 1, [FromQuery] int pageSize = GetFilesQuery.MaxPageSize, CancellationToken cancellationToken = default) =>
        await sender.Send(new GetFilesQuery(page, pageSize), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.FilesWrite)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadOptions.RequestLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadOptions.RequestLimitBytes)]
    public async Task<ActionResult<UploadedFileResponse>> Upload(
        IFormFile? file,
        [FromForm] Guid? propertyId,
        [FromForm] string? description,
        [FromForm] string? altText,
        [FromForm] bool isPublic = true,
        CancellationToken cancellationToken = default)
    {
        await using var content = file?.OpenReadStream() ?? Stream.Null;
        var id = await sender.Send(
            new UploadFileCommand(content, file?.FileName ?? string.Empty, file?.ContentType, file?.Length ?? 0, propertyId, description, altText, isPublic),
            cancellationToken);
        return Created($"/api/admin/files/{id}/content", new UploadedFileResponse(id));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.FilesWrite)]
    public async Task<IActionResult> Update(Guid id, UpdateFileRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateFileCommand(id, request.PropertyId, request.Description, request.AltText, request.IsPublic), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.FilesWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteFileCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/content")]
    [Authorize(Policy = Permissions.FilesRead)]
    public async Task<IActionResult> Content(Guid id, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new GetFileContentQuery(id), cancellationToken);
        Response.Headers.CacheControl = "private, no-store";
        return File(file.Stream, file.ContentType, file.FileName, enableRangeProcessing: true);
    }
}

[ApiController]
[Route("api/admin/properties/{propertyId:guid}/media")]
public sealed class AdminGalleryController(ISender sender) : ControllerBase
{
    [HttpPut("order")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<IActionResult> Reorder(Guid propertyId, ReorderMediaRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ReorderMediaCommand(propertyId, request.MediaIds), cancellationToken);
        return NoContent();
    }

    [HttpPut("{mediaId:guid}/cover")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<IActionResult> Cover(Guid propertyId, Guid mediaId, CancellationToken cancellationToken)
    {
        await sender.Send(new SetCoverCommand(propertyId, mediaId), cancellationToken);
        return NoContent();
    }
}

[ApiController]
[AllowAnonymous]
[Route("api/public/media")]
public sealed class PublicMediaController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new GetPublicMediaQuery(id), cancellationToken);
        // O conteúdo de um id nunca muda: cache longo e imutável.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(file.Stream, file.ContentType, enableRangeProcessing: true);
    }
}
