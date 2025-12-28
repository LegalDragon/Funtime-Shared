namespace Funtime.Identity.Api.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Storage type identifier ("local" or "s3")
    /// </summary>
    string StorageType { get; }

    /// <summary>
    /// Upload a file with asset ID as the filename
    /// Path structure: siteKey/YYYY-MM/assetId.extension
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="assetId">The asset ID to use as filename</param>
    /// <param name="siteKey">Site key for folder organization (defaults to "Shared")</param>
    Task<string> UploadFileAsync(IFormFile file, int assetId, string? siteKey = null);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Get the file stream for a stored file (for serving)
    /// </summary>
    Task<Stream?> GetFileStreamAsync(string fileUrl);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(string fileUrl);
}
