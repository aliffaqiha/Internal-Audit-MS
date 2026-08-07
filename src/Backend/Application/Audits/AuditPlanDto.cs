using IAMS.Domain.Enums;

namespace IAMS.Application.Audits;

public sealed record AuditPlanAssignmentDto(
    Guid UserId,
    string Username,
    string FullName,
    string? RoleInPlan);

public sealed record AuditPlanChecklistItemDto(
    Guid Id,
    string Question,
    string? Category,
    bool IsRequired,
    ChecklistItemStatus Status,
    string? Note);

public sealed record AuditPlanDto(
    Guid Id,
    string Title,
    string? Objective,
    string? Scope,
    string? Standard,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    AuditPlanStatus Status,
    Guid? DepartmentId,
    string? DepartmentName,
    IReadOnlyList<AuditPlanAssignmentDto> Assignments,
    IReadOnlyList<AuditPlanChecklistItemDto> ChecklistItems);

/// <summary>Default checklist templates per standard so a plan is always "runnable".</summary>
public static class StandardChecklistTemplates
{
    private static readonly (string Category, string Question)[] ItItems =
    {
        ("Backup", "Backup data otomatis terjadwal dan tercatat di lokasi terpisah."),
        ("Backup", "Backup rutin diuji pemulihan (restore test) secara berkala."),
        ("Firewall", "Firewall aktif pada perimeter jaringan dan konfigurasinya terdokumentasi."),
        ("Firewall", "Perubahan aturan firewall melalui proses approval dan ditinjau berkala."),
        ("Access Control", "Hak akses pengguna sesuai prinsip least-privilege."),
        ("Access Control", "Akun bekas karyawan dinonaktifkan atau dihapus tepat waktu."),
        ("Patch", "Server dan aplikasi menerapkan patch keamanan terkini."),
        ("Patch", "Asset lunak diaudit untuk versi dan kerentanan dikenal secara berkala.")
    };

    public static IReadOnlyList<(string Category, string Question)> ForStandard(string? standard)
        => IsIt(standard) ? ItItems : Array.Empty<(string, string)>();

    public static bool IsIt(string? standard)
        => !string.IsNullOrWhiteSpace(standard)
           && standard.Contains("IT", StringComparison.OrdinalIgnoreCase);
}