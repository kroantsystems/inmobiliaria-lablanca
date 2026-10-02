namespace LaBlanca.Application.Authorization;

public static class Permissions
{
    public const string ClaimType = "permission";

    public const string DashboardRead = "dashboard.read";
    public const string PropertiesRead = "properties.read";
    public const string PropertiesWrite = "properties.write";
    public const string FilesRead = "files.read";
    public const string FilesWrite = "files.write";
    public const string LeadsRead = "leads.read";
    public const string LeadsWrite = "leads.write";
    public const string OwnersRead = "owners.read";
    public const string OwnersWrite = "owners.write";
    public const string VisitsRead = "visits.read";
    public const string VisitsWrite = "visits.write";
    public const string SettingsRead = "settings.read";
    public const string SettingsWrite = "settings.write";

    public static readonly IReadOnlyList<string> All =
    [
        DashboardRead,
        PropertiesRead, PropertiesWrite,
        FilesRead, FilesWrite,
        LeadsRead, LeadsWrite,
        OwnersRead, OwnersWrite,
        VisitsRead, VisitsWrite,
        SettingsRead, SettingsWrite,
    ];
}

public static class Roles
{
    public const string Admin = "Admin";

    public static IReadOnlyList<string> PermissionsOf(string role) => role == Admin ? Permissions.All : [];
}
