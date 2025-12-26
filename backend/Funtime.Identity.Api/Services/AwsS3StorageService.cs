using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Funtime.Identity.Api.Services;

public class AwsS3StorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly bool _organizeByMonth;

    public string StorageType => "s3";

    public AwsS3StorageService(IConfiguration configuration)
    {
        var awsConfig = configuration.GetSection("AWS");
        _bucketName = awsConfig["BucketName"] ?? "funtime-identity";
        _organizeByMonth = configuration.GetValue("Storage:OrganizeByMonth", true);

        // In production, use IAM roles. For development, use credentials:
        _s3Client = new AmazonS3Client(
            awsConfig["AccessKey"],
            awsConfig["SecretKey"],
            Amazon.RegionEndpoint.GetBySystemName(awsConfig["Region"] ?? "us-east-1")
        );
    }

    public async Task<string> UploadFileAsync(IFormFile file, string containerName, string? siteKey = null)
    {
        // Build the S3 key: containerName / [siteKey] / [YYYY-MM] / filename
        var keyParts = new List<string> { containerName };

        // Add site subfolder if provided
        if (!string.IsNullOrEmpty(siteKey))
        {
            keyParts.Add(siteKey);
        }

        // Add month subfolder if enabled
        if (_organizeByMonth)
        {
            keyParts.Add(DateTime.UtcNow.ToString("yyyy-MM"));
        }

        // Add the filename
        keyParts.Add($"{Guid.NewGuid()}-{SanitizeFileName(file.FileName)}");

        var key = string.Join("/", keyParts);

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

    /// <summary>
    /// Sanitize a filename to remove potentially dangerous characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // S3 allows most characters, but we want clean filenames
        var invalidChars = new[] { '\\', '"', '<', '>', '|', '\0', '\n', '\r' };
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrEmpty(sanitized) ? "file" : sanitized;
    }
}
