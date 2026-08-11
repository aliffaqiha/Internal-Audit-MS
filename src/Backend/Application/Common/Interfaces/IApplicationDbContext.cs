using IAMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Common.Interfaces;

/// <summary>Abstraction over the application database, consumed by handlers/queries.</summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Department> Departments { get; }
    DbSet<AuditPlan> AuditPlans { get; }
    DbSet<AuditAssignment> AuditAssignments { get; }
    DbSet<AuditChecklistItem> AuditChecklistItems { get; }
    DbSet<Finding> Findings { get; }
    DbSet<FindingEvidence> FindingEvidences { get; }
    DbSet<CorrectiveAction> CorrectiveActions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}