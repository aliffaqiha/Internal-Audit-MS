using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        string action,
        string entity,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.Username,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            IpAddress = _currentUser.IpAddress,
            OldValues = oldValues,
            NewValues = newValues
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}