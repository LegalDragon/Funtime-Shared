using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("admin/asset-file-types")]
public class AssetFileTypeController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AssetFileTypeController> _logger;
    private readonly string _connectionString;

    public AssetFileTypeController(IConfiguration configuration, ILogger<AssetFileTypeController> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Get all enabled file types (for upload modal)
    /// </summary>
    [HttpGet("enabled")]
    [AllowAnonymous]
    public async Task<ActionResult<AssetFileTypesResponse>> GetEnabledFileTypes()
    {
        try
        {
            using var conn = CreateConnection();
            var fileTypes = (await conn.QueryAsync<AssetFileType>("exec dbo.csp_AssetFileTypes_GetEnabled")).ToList();

            var response = new AssetFileTypesResponse
            {
                FileTypes = fileTypes,
                AcceptString = BuildAcceptString(fileTypes),
                ByCategory = fileTypes.GroupBy(f => f.Category)
                    .ToDictionary(g => g.Key, g => g.ToList())
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enabled file types");
            return StatusCode(500, new { message = "Failed to get file types" });
        }
    }

    /// <summary>
    /// Get all file types (admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<List<AssetFileType>>> GetAll()
    {
        try
        {
            using var conn = CreateConnection();
            var fileTypes = await conn.QueryAsync<AssetFileType>("exec dbo.csp_AssetFileTypes_GetAll");
            return fileTypes.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all file types");
            return StatusCode(500, new { message = "Failed to get file types" });
        }
    }

    /// <summary>
    /// Get file type by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<AssetFileType>> GetById(int id)
    {
        try
        {
            using var conn = CreateConnection();
            var fileType = await conn.QuerySingleOrDefaultAsync<AssetFileType>(
                "exec dbo.csp_AssetFileTypes_GetById @Id",
                new { Id = id });

            if (fileType == null)
            {
                return NotFound(new { message = "File type not found" });
            }

            return fileType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file type {Id}", id);
            return StatusCode(500, new { message = "Failed to get file type" });
        }
    }

    /// <summary>
    /// Get file types by category
    /// </summary>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AssetFileType>>> GetByCategory(string category)
    {
        try
        {
            using var conn = CreateConnection();
            var fileTypes = await conn.QueryAsync<AssetFileType>(
                "exec dbo.csp_AssetFileTypes_GetByCategory @Category",
                new { Category = category });
            return fileTypes.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file types for category {Category}", category);
            return StatusCode(500, new { message = "Failed to get file types" });
        }
    }

    /// <summary>
    /// Create a new file type
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<AssetFileType>> Create([FromBody] CreateAssetFileTypeRequest request)
    {
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QuerySingleOrDefaultAsync<int?>(
                "exec dbo.csp_AssetFileTypes_Create @MimeType, @Extensions, @Category, @MaxSizeMB, @IsEnabled, @DisplayName",
                new
                {
                    request.MimeType,
                    request.Extensions,
                    request.Category,
                    request.MaxSizeMB,
                    request.IsEnabled,
                    request.DisplayName
                });

            if (result == null)
            {
                return BadRequest(new { message = "Failed to create file type. MIME type may already exist." });
            }

            _logger.LogInformation("File type {MimeType} created with ID {Id}", request.MimeType, result);

            return CreatedAtAction(nameof(GetById), new { id = result }, new AssetFileType
            {
                Id = result.Value,
                MimeType = request.MimeType,
                Extensions = request.Extensions,
                Category = request.Category,
                MaxSizeMB = request.MaxSizeMB,
                IsEnabled = request.IsEnabled,
                DisplayName = request.DisplayName,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (SqlException ex) when (ex.Message.Contains("already exists"))
        {
            return BadRequest(new { message = "MIME type already exists" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create file type");
            return StatusCode(500, new { message = "Failed to create file type" });
        }
    }

    /// <summary>
    /// Update a file type
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult<AssetFileType>> Update(int id, [FromBody] UpdateAssetFileTypeRequest request)
    {
        try
        {
            using var conn = CreateConnection();
            var rowsAffected = await conn.QuerySingleOrDefaultAsync<int>(
                "exec dbo.csp_AssetFileTypes_Update @Id, @MimeType, @Extensions, @Category, @MaxSizeMB, @IsEnabled, @DisplayName",
                new
                {
                    Id = id,
                    request.MimeType,
                    request.Extensions,
                    request.Category,
                    request.MaxSizeMB,
                    request.IsEnabled,
                    request.DisplayName
                });

            if (rowsAffected == 0)
            {
                return NotFound(new { message = "File type not found" });
            }

            _logger.LogInformation("File type {Id} updated", id);

            return Ok(new AssetFileType
            {
                Id = id,
                MimeType = request.MimeType,
                Extensions = request.Extensions,
                Category = request.Category,
                MaxSizeMB = request.MaxSizeMB,
                IsEnabled = request.IsEnabled,
                DisplayName = request.DisplayName,
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (SqlException ex) when (ex.Message.Contains("already exists"))
        {
            return BadRequest(new { message = "MIME type already exists for another file type" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update file type {Id}", id);
            return StatusCode(500, new { message = "Failed to update file type" });
        }
    }

    /// <summary>
    /// Delete a file type
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            using var conn = CreateConnection();
            var rowsAffected = await conn.QuerySingleOrDefaultAsync<int>(
                "exec dbo.csp_AssetFileTypes_Delete @Id",
                new { Id = id });

            if (rowsAffected == 0)
            {
                return NotFound(new { message = "File type not found" });
            }

            _logger.LogInformation("File type {Id} deleted", id);
            return Ok(new { message = "File type deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file type {Id}", id);
            return StatusCode(500, new { message = "Failed to delete file type" });
        }
    }

    /// <summary>
    /// Toggle file type enabled status
    /// </summary>
    [HttpPost("{id:int}/toggle")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult> Toggle(int id)
    {
        try
        {
            using var conn = CreateConnection();
            var isEnabled = await conn.QuerySingleOrDefaultAsync<bool?>(
                "exec dbo.csp_AssetFileTypes_ToggleEnabled @Id",
                new { Id = id });

            if (isEnabled == null)
            {
                return NotFound(new { message = "File type not found" });
            }

            _logger.LogInformation("File type {Id} toggled to {IsEnabled}", id, isEnabled);
            return Ok(new { isEnabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle file type {Id}", id);
            return StatusCode(500, new { message = "Failed to toggle file type" });
        }
    }

    /// <summary>
    /// Build accept string for file input from file types
    /// </summary>
    private static string BuildAcceptString(List<AssetFileType> fileTypes)
    {
        var acceptParts = new List<string>();

        foreach (var ft in fileTypes)
        {
            // Add MIME type
            acceptParts.Add(ft.MimeType);

            // Add extensions
            if (!string.IsNullOrEmpty(ft.Extensions))
            {
                acceptParts.AddRange(ft.Extensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim()));
            }
        }

        return string.Join(",", acceptParts.Distinct());
    }
}
