namespace IAMS.Application.Common.Interfaces;

/// <summary>Provides an abstraction over the current user's identity in the request context.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}

public sealed class CurrentUserContext : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
}