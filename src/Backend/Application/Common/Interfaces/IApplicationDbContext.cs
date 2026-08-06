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
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}