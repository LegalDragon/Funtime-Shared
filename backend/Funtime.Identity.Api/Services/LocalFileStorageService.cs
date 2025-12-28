namespace Funtime.Identity.Api.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _basePath;
    private readonly string _baseUrl;

    public string StorageType => "local";

    public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;

        // Configurable base path - defaults to wwwroot/uploads
        _basePath = configuration["Storage:LocalPath"] ??
            Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");

        // Base URL prefix for serving files (e.g., "" or "https://cdn.example.com")
        _baseUrl = configuration["Storage:LocalBaseUrl"] ?? "";
    }

    public async Task<string> UploadFileAsync(IFormFile file, int assetId, string? siteKey = null)
    {
        // Default siteKey to "Shared" if not provided
        var effectiveSiteKey = string.IsNullOrWhiteSpace(siteKey) ? "Shared" : siteKey;

        // Monthly folder
        var monthFolder = DateTime.UtcNow.ToString("yyyy-MM");

        // Build path: basePath/siteKey/YYYY-MM/
        var uploadsPath = Path.Combine(_basePath, effectiveSiteKey, monthFolder);
        Directory.CreateDirectory(uploadsPath);

        // Filename: assetId.extension
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{assetId}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Build the relative URL: /uploads/siteKey/YYYY-MM/assetId.ext
        var relativeUrl = $"/uploads/{effectiveSiteKey}/{monthFolder}/{fileName}";

        // Return with base URL if configured
        return string.IsNullOrEmpty(_baseUrl) ? relativeUrl : _baseUrl + relativeUrl;
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;

        var filePath = GetFilePathFromUrl(fileUrl);
        if (filePath != null && File.Exists(filePath))
        {
            File.Delete(filePath);

            // Try to clean up empty directories
            CleanupEmptyDirectories(Path.GetDirectoryName(filePath));
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> GetFileStreamAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.FromResult<Stream?>(null);

        var filePath = GetFilePathFromUrl(fileUrl);
        if (filePath == null || !File.Exists(filePath)) return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> FileExistsAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.FromResult(false);

        var filePath = GetFilePathFromUrl(fileUrl);
        return Task.FromResult(filePath != null && File.Exists(filePath));
    }

    /// <summary>
    /// Convert a URL to a file path
    /// </summary>
    private string? GetFilePathFromUrl(string fileUrl)
    {
        // Remove base URL if present
        if (!string.IsNullOrEmpty(_baseUrl) && fileUrl.StartsWith(_baseUrl))
        {
            fileUrl = fileUrl.Substring(_baseUrl.Length);
        }

        // Handle relative URLs starting with /uploads/
        if (fileUrl.StartsWith("/uploads/"))
        {
            var relativePath = fileUrl.Substring("/uploads/".Length);
            return Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        // Legacy support: handle URLs starting with just /
        if (fileUrl.StartsWith("/"))
        {
            var webRootPath = _environment.WebRootPath ?? "wwwroot";
            return Path.Combine(webRootPath, fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }

        return null;
    }

    /// <summary>
    /// Remove empty parent directories up to the base uploads path
    /// </summary>
    private void CleanupEmptyDirectories(string? directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath)) return;

        try
        {
            // Don't delete the base path itself
            while (!string.IsNullOrEmpty(directoryPath) &&
                   directoryPath.Length > _basePath.Length &&
                   Directory.Exists(directoryPath) &&
                   !Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                Directory.Delete(directoryPath);
                directoryPath = Path.GetDirectoryName(directoryPath);
            }
        }
        catch
        {
            // Ignore cleanup errors - not critical
        }
    }
}
