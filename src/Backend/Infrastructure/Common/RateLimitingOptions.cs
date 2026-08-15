namespace IAMS.Infrastructure.Common;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Global per-IP request limit applied to all endpoints (no named policy).</summary>
    public int GlobalPermitLimit { get; set; } = 300;

    /// <summary>Window (minutes) for the global per-IP limit.</summary>
    public int GlobalWindowMinutes { get; set; } = 1;

    /// <summary>Stricter per-IP limit for the login endpoint (anti brute-force).</summary>
    public int LoginPermitLimit { get; set; } = 10;

    /// <summary>Window (minutes) for the login limit.</summary>
    public int LoginWindowMinutes { get; set; } = 1;
}
