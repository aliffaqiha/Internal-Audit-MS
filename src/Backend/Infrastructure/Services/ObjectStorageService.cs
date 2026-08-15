using IAMS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace IAMS.Infrastructure.Common;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "iams";
    public bool UseSsl { get; set; }
}

public sealed class ObjectStorageService : IObjectStorageService, IDisposable
{
    /// <summary>Absolute per-object upload cap as a last line of defense.</summary>
    public const long MaxUploadBytes = 64 * 1024 * 1024; // 64 MB

    private readonly IMinioClient _client;
    private readonly string _bucket;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketReady;

    public ObjectStorageService(IOptions<MinioOptions> options)
    {
        var o = options.Value;
        _bucket = string.IsNullOrWhiteSpace(o.Bucket) ? "iams" : o.Bucket;
        _client = new MinioClient()
            .WithEndpoint(o.Endpoint)
            .WithCredentials(o.AccessKey, o.SecretKey)
            .WithSSL(o.UseSsl)
            .Build();
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady)
            return;

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketReady)
                return;

            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucket), cancellationToken);
            if (!exists)
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucket), cancellationToken);

            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    public async Task UploadAsync(string objectName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (content.Length > MaxUploadBytes)
            throw new InvalidOperationException($"Object exceeds the maximum allowed size of {MaxUploadBytes / (1024 * 1024)} MB.");

        await EnsureBucketAsync(cancellationToken);

        var args = new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithObjectSize(content.Length)
            .WithContentType(contentType)
            .WithStreamData(content);

        await _client.PutObjectAsync(args, cancellationToken);
    }

    public async Task<Stream?> GetAsync(string objectName, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        var ms = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(ms));

        await _client.GetObjectAsync(args, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string objectName, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        try
        {
            await _client.RemoveObjectAsync(
                new RemoveObjectArgs().WithBucket(_bucket).WithObject(objectName), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Deleting a missing object should not fail the delete operation.
        }
    }

    public async Task<bool> ExistsAsync(string objectName, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        try
        {
            await _client.StatObjectAsync(
                new StatObjectArgs().WithBucket(_bucket).WithObject(objectName), cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _bucketLock.Dispose();
    }
}