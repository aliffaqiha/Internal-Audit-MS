namespace IAMS.Application.Common.Interfaces;

/// <summary>Abstraction over object storage (MinIO) used for evidence files.</summary>
public interface IObjectStorageService
{
    Task UploadAsync(string objectName, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Streams an object back to the caller; returns null if not found.</summary>
    Task<Stream?> GetAsync(string objectName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectName, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string objectName, CancellationToken cancellationToken = default);
}