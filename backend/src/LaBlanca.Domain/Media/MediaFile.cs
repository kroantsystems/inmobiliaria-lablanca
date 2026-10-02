using LaBlanca.Domain.Common;

namespace LaBlanca.Domain.Media;

public enum MediaKind
{
    Image,
    Video,
    Document,
}

public sealed class MediaFile : TenantEntity
{
    private MediaFile()
    {
    }

    public Guid? PropertyId { get; private set; }

    public string OriginalName { get; private set; } = string.Empty;

    public string StorageKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public MediaKind Kind { get; private set; }

    public long SizeBytes { get; private set; }

    public string? Description { get; private set; }

    public string? AltText { get; private set; }

    public bool IsPublic { get; private set; }

    public bool IsCover { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public Guid UploadedBy { get; private set; }

    public static MediaFile Create(
        Guid? propertyId,
        string originalName,
        string storageKey,
        string contentType,
        MediaKind kind,
        long sizeBytes,
        Guid uploadedBy,
        DateTimeOffset uploadedAt,
        bool isPublic) => new()
        {
            PropertyId = propertyId,
            OriginalName = originalName,
            StorageKey = storageKey,
            ContentType = contentType,
            Kind = kind,
            SizeBytes = sizeBytes,
            UploadedBy = uploadedBy,
            UploadedAt = uploadedAt,
            IsPublic = isPublic && kind != MediaKind.Document,
        };

    public void UpdateDetails(string? description, string? altText)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
    }

    public void SetPublic(bool isPublic)
    {
        if (isPublic && Kind == MediaKind.Document)
        {
            throw new DomainException(DomainErrors.DocumentsArePrivate);
        }

        IsPublic = isPublic;
    }

    public void Unlink()
    {
        PropertyId = null;
        IsCover = false;
    }

    internal void LinkTo(Guid propertyId)
    {
        if (PropertyId != propertyId)
        {
            IsCover = false;
        }

        PropertyId = propertyId;
    }

    internal void MarkAsCover()
    {
        if (Kind != MediaKind.Image)
        {
            throw new DomainException(DomainErrors.OnlyImagesCanBeCover);
        }

        IsCover = true;
    }

    internal void UnmarkCover() => IsCover = false;

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
