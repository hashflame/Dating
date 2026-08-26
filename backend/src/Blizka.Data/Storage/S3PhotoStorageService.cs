using Amazon.S3;
using Amazon.S3.Model;
using Blizka.App.Domain.Services;
using Microsoft.Extensions.Options;

namespace Blizka.Data.Storage;

/// <summary>
/// Реализация <see cref="IPhotoStorageService"/> поверх AWSSDK.S3 — работает как с реальным S3, так и с MinIO
/// (используется в docker-compose.yml для локальной разработки), т.к. MinIO реализует тот же S3 API.
/// </summary>
public sealed class S3PhotoStorageService(IAmazonS3 s3Client, IOptions<StorageOptions> options) : IPhotoStorageService
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await s3Client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = true,
            },
            cancellationToken);

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    public async Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        using var response = await s3Client.GetObjectAsync(_options.Bucket, key, cancellationToken);
        using var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        s3Client.DeleteObjectAsync(_options.Bucket, key, cancellationToken);

    public Task<string> GetTemporaryDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken cancellationToken) =>
        s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(validFor),
        });
}
