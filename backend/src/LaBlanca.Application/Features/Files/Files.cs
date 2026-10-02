using FluentValidation;
using LaBlanca.Application.Abstractions.Files;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Revalidation;
using LaBlanca.Application.Abstractions.Security;
using LaBlanca.Application.Features.Properties;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Media;
using LaBlanca.Domain.Properties;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Files;

public sealed record FileListItem(
    Guid Id,
    string OriginalName,
    string ContentType,
    MediaKind Kind,
    long SizeBytes,
    string? Description,
    string? AltText,
    bool IsPublic,
    bool IsCover,
    Guid? PropertyId,
    string? PropertyTitle,
    DateTimeOffset UploadedAt,
    string Url,
    string? PublicUrl);

public sealed record UploadFileCommand(
    Stream Content,
    string FileName,
    string? ContentType,
    long Length,
    Guid? PropertyId,
    string? Description,
    string? AltText,
    bool IsPublic = true) : ICommand<Guid>;

public sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.Length).GreaterThan(0).WithMessage(_ => AppMessages.Get(MessageKeys.FileRequired));
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AltText).MaximumLength(200);
    }
}

public sealed class UploadFileCommandHandler(
    IAppDbContext db,
    IFileInspector inspector,
    IFileStorage storage,
    ICurrentUser currentUser,
    IRevalidationNotifier revalidation,
    TimeProvider clock) : IRequestHandler<UploadFileCommand, Guid>
{
    public async Task<Guid> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var inspection = inspector.Inspect(request.FileName, request.ContentType, request.Length, request.Content);
        if (!inspection.IsValid)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["file"] = [AppMessages.Get(inspection.ErrorKey!, inspection.ErrorArgs)],
            });
        }

        Property? property = null;
        if (request.PropertyId is { } propertyId)
        {
            property = await db.Properties.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken)
                ?? throw new BusinessRuleException(MessageKeys.NotFound);
        }

        var key = await storage.SaveAsync(request.Content, inspection.Extension, cancellationToken);
        try
        {
            var media = MediaFile.Create(
                null,
                Path.GetFileName(request.FileName),
                key,
                inspection.ContentType,
                inspection.Kind,
                request.Length,
                currentUser.UserId ?? Guid.Empty,
                clock.GetUtcNow(),
                request.IsPublic);
            media.UpdateDetails(request.Description, request.AltText);

            if (property is null)
            {
                db.MediaFiles.Add(media);
            }
            else
            {
                property.AddMedia(media);
                revalidation.Request(CacheTags.ForProperty(property.Id));
            }

            await db.SaveChangesAsync(cancellationToken);
            return media.Id;
        }
        catch
        {
            await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }
    }
}

public sealed record GetFilesQuery(int Page = 1, int PageSize = GetFilesQuery.MaxPageSize) : IQuery<PagedResult<FileListItem>>
{
    public const int MaxPageSize = 500;
}

public sealed class GetFilesQueryHandler(IAppDbContext db) : IRequestHandler<GetFilesQuery, PagedResult<FileListItem>>
{
    public async Task<PagedResult<FileListItem>> Handle(GetFilesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, GetFilesQuery.MaxPageSize);
        var page = Math.Max(request.Page, 1);
        var total = await db.MediaFiles.CountAsync(cancellationToken);
        var files = await db.MediaFiles.AsNoTracking().OrderByDescending(m => m.UploadedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var propertyIds = files.Where(f => f.PropertyId != null).Select(f => f.PropertyId!.Value).Distinct().ToList();
        var titles = await db.PropertyTranslations.AsNoTracking()
            .Where(t => propertyIds.Contains(t.PropertyId) && t.Locale == Locales.Default)
            .ToDictionaryAsync(t => t.PropertyId, t => t.Title, cancellationToken);

        return new PagedResult<FileListItem>(
            [.. files.Select(f => new FileListItem(
                f.Id, f.OriginalName, f.ContentType, f.Kind, f.SizeBytes, f.Description, f.AltText, f.IsPublic, f.IsCover, f.PropertyId,
                f.PropertyId is { } id ? titles.GetValueOrDefault(id) : null, f.UploadedAt, MediaUrls.Admin(f.Id),
                f.IsPublic && f.PropertyId != null ? MediaUrls.Public(f.Id) : null))],
            total,
            page,
            pageSize);
    }
}

public sealed record UpdateFileCommand(Guid Id, Guid? PropertyId, string? Description, string? AltText, bool IsPublic) : ICommand;

public sealed class UpdateFileCommandValidator : AbstractValidator<UpdateFileCommand>
{
    public UpdateFileCommandValidator()
    {
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AltText).MaximumLength(200);
    }
}

internal static class GalleryGuard
{
    /// <summary>Impede deixar um anúncio publicado sem nenhuma imagem pública.</summary>
    public static async Task EnsureNotLastPublicImageAsync(this IAppDbContext db, MediaFile media, CancellationToken cancellationToken)
    {
        if (media.PropertyId is not { } propertyId || media.Kind != MediaKind.Image || !media.IsPublic)
        {
            return;
        }

        var published = await db.Properties.AnyAsync(p => p.Id == propertyId && p.IsPublished, cancellationToken);
        var others = await db.MediaFiles.AnyAsync(
            m => m.PropertyId == propertyId && m.Id != media.Id && m.Kind == MediaKind.Image && m.IsPublic, cancellationToken);
        if (published && !others)
        {
            throw new ConflictException(MessageKeys.LastPublicImage);
        }
    }
}

public sealed class UpdateFileCommandHandler(IAppDbContext db, IRevalidationNotifier revalidation) : IRequestHandler<UpdateFileCommand>
{
    public async Task Handle(UpdateFileCommand request, CancellationToken cancellationToken)
    {
        var media = await db.MediaFiles.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken) ?? throw new NotFoundException();
        var previousProperty = media.PropertyId;

        if (previousProperty != request.PropertyId || (media.IsPublic && !request.IsPublic))
        {
            await db.EnsureNotLastPublicImageAsync(media, cancellationToken);
        }

        media.UpdateDetails(request.Description, request.AltText);
        media.SetPublic(request.IsPublic);

        if (previousProperty != request.PropertyId)
        {
            media.Unlink();
            if (request.PropertyId is { } propertyId)
            {
                var property = await db.Properties.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken)
                    ?? throw new BusinessRuleException(MessageKeys.NotFound);
                property.AddMedia(media);
            }
        }

        foreach (var id in new[] { previousProperty, request.PropertyId }.OfType<Guid>().Distinct())
        {
            revalidation.Request(CacheTags.ForProperty(id));
        }
    }
}

public sealed record DeleteFileCommand(Guid Id) : ICommand;

public sealed class DeleteFileCommandHandler(IAppDbContext db, IFileStorage storage, IRevalidationNotifier revalidation) : IRequestHandler<DeleteFileCommand>
{
    public async Task Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var media = await db.MediaFiles.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken) ?? throw new NotFoundException();
        await db.EnsureNotLastPublicImageAsync(media, cancellationToken);

        db.MediaFiles.Remove(media);
        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(media.StorageKey, cancellationToken);

        if (media.PropertyId is { } propertyId)
        {
            revalidation.Request(CacheTags.ForProperty(propertyId));
        }
    }
}

public sealed record SetCoverCommand(Guid PropertyId, Guid MediaId) : ICommand;

public sealed class SetCoverCommandHandler(IAppDbContext db, IRevalidationNotifier revalidation) : IRequestHandler<SetCoverCommand>
{
    public async Task Handle(SetCoverCommand request, CancellationToken cancellationToken)
    {
        var property = await db.Properties.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException();
        property.SetCover(request.MediaId);
        revalidation.Request(CacheTags.ForProperty(property.Id));
    }
}

public sealed record ReorderMediaCommand(Guid PropertyId, IReadOnlyList<Guid> MediaIds) : ICommand;

public sealed class ReorderMediaCommandHandler(IAppDbContext db, IRevalidationNotifier revalidation) : IRequestHandler<ReorderMediaCommand>
{
    public async Task Handle(ReorderMediaCommand request, CancellationToken cancellationToken)
    {
        var property = await db.Properties.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken)
            ?? throw new NotFoundException();
        property.ReorderMedia(request.MediaIds);
        revalidation.Request(CacheTags.ForProperty(property.Id));
    }
}

public sealed record GetFileContentQuery(Guid Id) : IQuery<FileContent>;

public sealed class GetFileContentQueryHandler(IAppDbContext db, IFileStorage storage) : IRequestHandler<GetFileContentQuery, FileContent>
{
    public async Task<FileContent> Handle(GetFileContentQuery request, CancellationToken cancellationToken)
    {
        var media = await db.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken) ?? throw new NotFoundException();
        return new FileContent(await storage.OpenReadAsync(media.StorageKey, cancellationToken), media.ContentType, media.OriginalName);
    }
}

/// <summary>Só imagens e vídeos públicos de anúncios publicados; documentos nunca.</summary>
public sealed record GetPublicMediaQuery(Guid Id) : IQuery<FileContent>;

public sealed class GetPublicMediaQueryHandler(IAppDbContext db, IFileStorage storage) : IRequestHandler<GetPublicMediaQuery, FileContent>
{
    public async Task<FileContent> Handle(GetPublicMediaQuery request, CancellationToken cancellationToken)
    {
        var media = await db.MediaFiles.AsNoTracking()
            .Where(m => m.Id == request.Id && m.IsPublic && m.Kind != MediaKind.Document && m.PropertyId != null)
            .Where(m => db.Properties.Any(p => p.Id == m.PropertyId && p.IsPublished))
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException();
        return new FileContent(await storage.OpenReadAsync(media.StorageKey, cancellationToken), media.ContentType, media.OriginalName);
    }
}
