using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace IAMS.Application.Analytics;

public sealed record GetDashboardAnalyticsQuery : IRequest<DashboardAnalyticsDto>;

internal sealed class GetDashboardAnalyticsQueryHandler : IRequestHandler<GetDashboardAnalyticsQuery, DashboardAnalyticsDto>
{
    private const string CacheKey = "dashboard:analytics";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeService _dateTime;
    private readonly IDistributedCache _cache;

    public GetDashboardAnalyticsQueryHandler(
        IApplicationDbContext db,
        IDateTimeService dateTime,
        IDistributedCache cache)
    {
        _db = db;
        _dateTime = dateTime;
        _cache = cache;
    }

    public async Task<DashboardAnalyticsDto> Handle(
        GetDashboardAnalyticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cached = await _cache.GetStringAsync(CacheKey, cancellationToken);
            if (cached is not null)
            {
                var parsed = JsonSerializer.Deserialize<DashboardAnalyticsDto>(cached);
                if (parsed is not null)
                    return parsed;
            }
        }
        catch (Exception)
        {
            // Redis unreachable -> compute fresh below.
        }

        var dto = await ComputeAsync(cancellationToken);

        try
        {
            await _cache.SetStringAsync(
                CacheKey,
                JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
                cancellationToken);
        }
        catch (Exception)
        {
            // Cache writes are best-effort.
        }

        return dto;
    }

    private async Task<DashboardAnalyticsDto> ComputeAsync(CancellationToken cancellationToken)
    {
        var plans = await _db.AuditPlans.AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new AuditPlanStatusSnapshot(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var findings = await _db.Findings.AsNoTracking()
            .Select(f => new FindingSnapshot(
                f.RiskLevel,
                f.Category,
                f.Department != null ? f.Department.Name : null))
            .ToListAsync(cancellationToken);

        var closedCaps = await _db.CorrectiveActions.AsNoTracking()
            .Where(c => c.Status == CorrectiveActionStatus.Closed && c.VerifiedAt.HasValue)
            .Select(c => new ClosedCapSnapshot(c.VerifiedAt!.Value, c.Finding!.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalOpenCaps = await _db.CorrectiveActions.AsNoTracking()
            .CountAsync(c => c.Status != CorrectiveActionStatus.Closed, cancellationToken);

        var openCapTargetDates = await _db.CorrectiveActions.AsNoTracking()
            .Where(c => c.Status != CorrectiveActionStatus.Closed && c.TargetDate.HasValue)
            .Select(c => c.TargetDate!.Value.Date)
            .ToListAsync(cancellationToken);

        var workload = await _db.AuditAssignments.AsNoTracking()
            .GroupBy(a => a.User!.FullName)
            .Select(g => new AuditorSnapshot(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return AnalyticsMetrics.Compute(
            plans,
            findings,
            closedCaps,
            totalOpenCaps,
            openCapTargetDates,
            workload,
            _dateTime.UtcNowOffset);
    }
}
