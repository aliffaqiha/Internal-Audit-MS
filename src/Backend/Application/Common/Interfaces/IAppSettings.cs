namespace IAMS.Application.Common.Interfaces;

/// <summary>
/// Provides application-level configuration consumed by domain/application logic
/// (e.g. the base URL used to build links placed in outbound emails).
/// </summary>
public interface IAppSettings
{
    /// <summary>Base URL of the client application (no trailing slash).</summary>
    string ClientBaseUrl { get; }
}
