namespace IAMS.Domain.Enums;

public enum SystemRole
{
    Administrator = 1,
    AuditManager = 2,
    Auditor = 3,
    Auditee = 4,
    TopManagement = 5
}

public static class RoleConstants
{
    public const string Administrator = "Administrator";
    public const string Manager = "AuditManager";
    public const string Auditor = "Auditor";
    public const string Auditee = "Auditee";
    public const string TopManagement = "TopManagement";

    public static string Normalize(string value) => value.ToUpperInvariant();
}