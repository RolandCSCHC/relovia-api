using Amazon.S3;
using Amazon.S3.Model;

namespace Relovia.Api.Features.Documents;

/// <summary>
/// S3-compatible storage for a private bucket. Neon Object Storage needs path-style addressing.
/// </summary>
public sealed class S3ObjectStorage(IAmazonS3 s3, S3StorageSettings settings) : IObjectStorage
{
    public async Task SaveAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        ObjectKeys.Require(key);
        var request = new PutObjectRequest
        {
            BucketName = settings.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };
        if (content.CanSeek)
        {
            request.Headers.ContentLength = content.Length - content.Position;
        }

        await s3.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken)
    {
        ObjectKeys.Require(key);
        GetObjectResponse response;
        try
        {
            response = await s3.GetObjectAsync(settings.Bucket, key, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound
            || exception.ErrorCode is "NoSuchKey" or "NotFound")
        {
            return null;
        }

        using (response)
        {
            var copy = new MemoryStream();
            await response.ResponseStream.CopyToAsync(copy, cancellationToken);
            copy.Position = 0;
            return copy;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        ObjectKeys.Require(key);
        return s3.DeleteObjectAsync(settings.Bucket, key, cancellationToken);
    }
}
