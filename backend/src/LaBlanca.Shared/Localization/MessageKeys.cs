namespace LaBlanca.Shared.Localization;

public static class MessageKeys
{
    public const string Unexpected = "Error.Unexpected";
    public const string Validation = "Error.Validation";
    public const string NotFound = "Error.NotFound";
    public const string Unauthorized = "Error.Unauthorized";
    public const string Forbidden = "Error.Forbidden";
    public const string TooManyRequests = "Error.TooManyRequests";

    public const string InvalidCredentials = "Auth.InvalidCredentials";
    public const string SessionExpired = "Auth.SessionExpired";
    public const string CurrentPasswordInvalid = "Auth.CurrentPasswordInvalid";
    public const string PasswordMustDiffer = "Auth.PasswordMustDiffer";
    public const string PasswordPolicy = "Auth.PasswordPolicy";
    public const string UserEmailExists = "Auth.UserEmailExists";

    public const string UnsupportedLocale = "Validation.UnsupportedLocale";
    public const string InternationalPhone = "Validation.InternationalPhone";
    public const string InvalidUrl = "Validation.InvalidUrl";
    public const string InvalidSlug = "Validation.InvalidSlug";
    public const string SpanishTranslationRequired = "Validation.SpanishTranslationRequired";
    public const string DuplicatedLocale = "Validation.DuplicatedLocale";

    public const string ZoneSlugExists = "Zone.SlugExists";
    public const string ZoneInUse = "Zone.InUse";

    public const string InvalidVideoUrl = "Property.InvalidVideoUrl";
    public const string PropertyZoneNotFound = "Property.ZoneNotFound";
    public const string PropertyOwnerNotFound = "Property.OwnerNotFound";

    public static string ProblemTitle(int statusCode) => $"Problem.Title.{statusCode}";
}
