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

    public static string ProblemTitle(int statusCode) => $"Problem.Title.{statusCode}";
}
