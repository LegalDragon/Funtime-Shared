using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Funtime.Identity.Api.Services;

public class AwsS3StorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public string StorageType => "s3";

    public AwsS3StorageService(IConfiguration configuration)
    {
        var awsConfig = configuration.GetSection("AWS");
        _bucketName = awsConfig["BucketName"] ?? "funtime-identity";

        // In production, use IAM roles. For development, use credentials:
        _s3Client = new AmazonS3Client(
            awsConfig["AccessKey"],
            awsConfig["SecretKey"],
            Amazon.RegionEndpoint.GetBySystemName(awsConfig["Region"] ?? "us-east-1")
        );
    }

    public async Task<string> UploadFileAsync(IFormFile file, int assetId, string? siteKey = null)
    {
        // Default siteKey to "Shared" if not provided
        var effectiveSiteKey = string.IsNullOrWhiteSpace(siteKey) ? "Shared" : siteKey;

        // Monthly folder
        var monthFolder = DateTime.UtcNow.ToString("yyyy-MM");

        // Filename: assetId.extension
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{assetId}{extension}";

        // Build the S3 key: siteKey/YYYY-MM/assetId.ext
        var key = $"{effectiveSiteKey}/{monthFolder}/{fileName}";

        using var stream = file.OpenReadStream();
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            Key = key,
            BucketName = _bucketName,
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.PublicRead
        };

        var transferUtility = new TransferUtility(_s3Client);
        await transferUtility.UploadAsync(uploadRequest);

        return $"https://{_bucketName}.s3.amazonaws.com/{key}";
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        var key = ExtractKeyFromUrl(fileUrl);
        if (key == null) return;

        await _s3Client.DeleteObjectAsync(_bucketName, key);
    }

    public async Task<Stream?> GetFileStreamAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return null;

        var key = ExtractKeyFromUrl(fileUrl);
        if (key == null) return null;

        try
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, key);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return false;

        var key = ExtractKeyFromUrl(fileUrl);
        if (key == null) return false;

        try
        {
            await _s3Client.GetObjectMetadataAsync(_bucketName, key);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private string? ExtractKeyFromUrl(string fileUrl)
    {
        // Handle both full S3 URLs and relative paths
        if (fileUrl.StartsWith("https://"))
        {
            var uri = new Uri(fileUrl);
            return uri.AbsolutePath.TrimStart('/');
        }
        return fileUrl.TrimStart('/');
    }
}
