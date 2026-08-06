namespace IAMS.Application.Common.Interfaces;

/// <summary>Records security/audit events for authentication-related actions.</summary>
public interface IAuditService
{
    Task LogAsync(
        string action,
        string entity,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default);
}