using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Common;
using IAMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace IAMS.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IDateTimeService _dateTime;
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDateTimeService dateTime,
        ICurrentUserService currentUser)
        : base(options)
    {
        _dateTime = dateTime;
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AuditPlan> AuditPlans => Set<AuditPlan>();
    public DbSet<AuditAssignment> AuditAssignments => Set<AuditAssignment>();
    public DbSet<AuditChecklistItem> AuditChecklistItems => Set<AuditChecklistItem>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<FindingEvidence> FindingEvidences => Set<FindingEvidence>();
    public DbSet<CorrectiveAction> CorrectiveActions => Set<CorrectiveAction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTime.UtcNowOffset;
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        NormalizeToUtc();

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        var now = _dateTime.UtcNowOffset;
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        NormalizeToUtc();

        return base.SaveChanges();
    }

    /// <summary>
    /// Npgsql only writes <see cref="DateTimeOffset"/> values with offset 0 (UTC) to
    /// "timestamp with time zone" columns. Normalize any non-UTC offsets (e.g. client
    /// supplied local times) before persisting.
    /// </summary>
    private void NormalizeToUtc()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            foreach (PropertyEntry property in entry.Properties)
            {
                if (property.Metadata.ClrType != typeof(DateTimeOffset)
                    && property.Metadata.ClrType != typeof(DateTimeOffset?))
                    continue;

                if (property.CurrentValue is not DateTimeOffset value)
                    continue;

                if (value.Offset != TimeSpan.Zero)
                    property.CurrentValue = value.ToUniversalTime();
            }
        }
    }
}