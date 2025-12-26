namespace Funtime.Identity.Api.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly bool _organizeByMonth;

    public string StorageType => "local";

    public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;

        // Configurable base path - defaults to wwwroot/uploads
        _basePath = configuration["Storage:LocalPath"] ??
            Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");

        // Base URL prefix for serving files (e.g., "" or "https://cdn.example.com")
        _baseUrl = configuration["Storage:LocalBaseUrl"] ?? "";

        // Whether to organize files into monthly subfolders
        _organizeByMonth = configuration.GetValue("Storage:OrganizeByMonth", true);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string containerName, string? siteKey = null)
    {
        // Build the path: basePath / containerName / [siteKey] / [YYYY-MM] / filename
        var pathParts = new List<string> { _basePath, containerName };
        var urlParts = new List<string> { "uploads", containerName };

        // Add site subfolder if provided
        if (!string.IsNullOrEmpty(siteKey))
        {
            pathParts.Add(siteKey);
            urlParts.Add(siteKey);
        }

        // Add month subfolder if enabled
        if (_organizeByMonth)
        {
            var monthFolder = DateTime.UtcNow.ToString("yyyy-MM");
            pathParts.Add(monthFolder);
            urlParts.Add(monthFolder);
        }

        var uploadsPath = Path.Combine(pathParts.ToArray());
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}-{SanitizeFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Build the relative URL
        var relativeUrl = "/" + string.Join("/", urlParts) + "/" + fileName;

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

    /// <summary>
    /// Sanitize a filename to remove potentially dangerous characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrEmpty(sanitized) ? "file" : sanitized;
    }
}
