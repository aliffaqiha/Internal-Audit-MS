using IAMS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IAMS.Infrastructure.Services;

/// <summary>Maintenance sweep removing stale auth tokens (idempotent by nature). Scheduled by Hangfire.</summary>
public interface ICleanupService
{
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
}

public sealed class CleanupService : ICleanupService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(IServiceScopeFactory scopeFactory, ILogger<CleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;

        // Revoked or expired refresh tokens retained for 30 days.
        var refreshCutoff = now.AddDays(-30);
        var staleRefresh = await db.RefreshTokens
            .Where(t => t.RevokedAt != null || t.ExpiresAt < refreshCutoff)
            .ToListAsync(cancellationToken);

        // Used or expired password reset tokens retained for 7 days.
        var resetCutoff = now.AddDays(-7);
        var staleReset = await db.PasswordResetTokens
            .Where(t => t.UsedAt != null || t.ExpiresAt < resetCutoff)
            .ToListAsync(cancellationToken);

        if (staleRefresh.Count == 0 && staleReset.Count == 0)
            return 0;

        db.RefreshTokens.RemoveRange(staleRefresh);
        db.PasswordResetTokens.RemoveRange(staleReset);
        var deleted = await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cleanup removed {Refresh} refresh tokens and {Reset} password reset tokens",
            staleRefresh.Count, staleReset.Count);

        return deleted;
    }
}
