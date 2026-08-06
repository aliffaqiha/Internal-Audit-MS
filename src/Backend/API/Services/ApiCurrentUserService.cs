using System.Security.Claims;
using IAMS.Application.Common.Interfaces;

namespace IAMS.Api.Services;

/// <summary>Resolves the current user from the HTTP context.</summary>
public sealed class ApiCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public ApiCurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : (Guid?)null;
        }
    }

    public string? Username => _accessor.HttpContext?.User?.Identity?.Name;

    public string? IpAddress => _accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}