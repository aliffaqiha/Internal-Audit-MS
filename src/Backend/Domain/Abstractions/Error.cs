namespace IAMS.Domain.Abstractions;

/// <summary>
/// Represents an error with a machine-readable code and human-readable message.
/// </summary>
public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");
    public static readonly Error NotFound = new("Error.NotFound", "The requested resource was not found.");
    public static readonly Error Conflict = new("Error.Conflict", "The request conflicts with the current state.");

    public static Error BadRequest(string message) => new("Error.BadRequest", message);
    public static Error Unauthorized(string message = "You are not authorized.") => new("Error.Unauthorized", message);
}