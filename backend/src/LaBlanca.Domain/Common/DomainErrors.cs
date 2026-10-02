namespace LaBlanca.Domain.Common;

/// <summary>Chaves de mensagem das regras de domínio; os textos ficam nos recursos do LaBlanca.Shared.</summary>
public static class DomainErrors
{
    public const string PropertyMustBeAvailableToPublish = "Property.MustBeAvailableToPublish";
    public const string PropertyPublishRequiresImage = "Property.PublishRequiresImage";
    public const string PropertyMustBePublishedToFeature = "Property.MustBePublishedToFeature";
    public const string MediaNotInProperty = "Media.NotInProperty";
    public const string DocumentsArePrivate = "Media.DocumentsArePrivate";
    public const string OnlyImagesCanBeCover = "Media.OnlyImagesCanBeCover";
}
