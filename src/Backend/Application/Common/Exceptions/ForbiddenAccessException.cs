namespace IAMS.Application.Common.Exceptions;

/// <summary>
/// Thrown when the current user is authenticated but not allowed to access a
/// resource (e.g. data outside their department). Maps to HTTP 403 Forbidden.
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
