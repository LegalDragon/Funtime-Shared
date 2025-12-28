namespace Funtime.Identity.Api.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Storage type identifier ("local" or "s3")
    /// </summary>
    string StorageType { get; }

    /// <summary>
    /// Upload a file and return the storage URL
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="containerName">Category/container name (e.g., "logos", "avatars")</param>
    /// <param name="siteKey">Optional site key for folder organization (e.g., "pickleball-community")</param>
    Task<string> UploadFileAsync(IFormFile file, string containerName, string? siteKey = null);

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
