using System.Net;

namespace IAMS.Application.Common.Exceptions;

/// <summary>Thrown by the validation behavior when command/query validation fails.</summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors have occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}