using IAMS.Application.Common.Interfaces;
using MediatR;

namespace IAMS.Application.Auth;

/// <summary>
/// Mints a short-lived JWT dedicated to authenticating the SignalR connection.
/// Kept separate from the main access token so the WebSocket query-string token
/// expires quickly and carries no roles or email.
/// </summary>
public sealed record GetSignalRTokenQuery : IRequest<SignalRTokenResponse>;

public sealed record SignalRTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt);

internal sealed class GetSignalRTokenQueryHandler : IRequestHandler<GetSignalRTokenQuery, SignalRTokenResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenProvider _tokenProvider;

    public GetSignalRTokenQueryHandler(ICurrentUserService currentUser, ITokenProvider tokenProvider)
    {
        _currentUser = currentUser;
        _tokenProvider = tokenProvider;
    }

    public Task<SignalRTokenResponse> Handle(GetSignalRTokenQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Unauthorized.");

        var info = _tokenProvider.CreateSignalRToken(userId, _currentUser.Username ?? string.Empty);
        return Task.FromResult(new SignalRTokenResponse(info.Token, info.ExpiresAt));
    }
}
