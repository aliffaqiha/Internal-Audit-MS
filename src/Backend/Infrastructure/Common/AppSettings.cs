using IAMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IAMS.Infrastructure.Common;

/// <summary>Reads application settings from configuration (see <c>AppUrls</c> section).</summary>
public sealed class AppSettings : IAppSettings
{
    private const string DefaultClientBaseUrl = "http://localhost:5173";

    public AppSettings(IConfiguration configuration)
    {
        var value = configuration["AppUrls:ClientBaseUrl"];
        ClientBaseUrl = (string.IsNullOrWhiteSpace(value) ? DefaultClientBaseUrl : value).TrimEnd('/');
    }

    public string ClientBaseUrl { get; }
}
