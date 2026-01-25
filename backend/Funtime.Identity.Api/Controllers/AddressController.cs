using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;
using System.Security.Claims;

namespace Funtime.Identity.Api.Controllers;

/// <summary>
/// Address management endpoints - standalone address registry for use by affiliate sites
/// </summary>
[ApiController]
[Route("addresses")]
public class AddressController : ControllerBase
{
    private readonly string _connectionString;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<AddressController> _logger;

    public AddressController(
        IConfiguration configuration,
        IGeocodingService geocodingService,
        ILogger<AddressController> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
        _geocodingService = geocodingService;
        _logger = logger;
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Get address by ID - returns full address with GPS and location hierarchy
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<AddressDetailResponse>> GetAddress(int id)
    {
        using var conn = CreateConnection();
        var address = await conn.QuerySingleOrDefaultAsync<AddressDetailResponse>(
            @"SELECT a.Id, a.Line1, a.Line2, a.PostalCode,
                     a.Latitude, a.Longitude, a.IsVerified,
                     a.CreatedAt, a.UpdatedAt,
                     c.Id AS CityId, c.Name AS CityName,
                     c.Latitude AS CityLatitude, c.Longitude AS CityLongitude,
                     p.Id AS ProvinceStateId, p.Name AS ProvinceStateName, p.Code AS ProvinceStateCode,
                     co.Id AS CountryId, co.Name AS CountryName, co.Code2 AS CountryCode
              FROM Addresses a
              JOIN Cities c ON a.CityId = c.Id
              JOIN ProvinceStates p ON c.ProvinceStateId = p.Id
              JOIN Countries co ON p.CountryId = co.Id
              WHERE a.Id = @Id", new { Id = id });

        if (address == null)
            return NotFound(new { message = "Address not found." });

        return Ok(address);
    }

    /// <summary>
    /// Create a new address or return existing if duplicate found.
    /// Duplicate detection: same cityId + line1 + postalCode (case-insensitive)
    /// </summary>
    [HttpPost]
    [ApiKeyAuthorize(ApiScopes.AddressesWrite, AllowJwt = true)]
    public async Task<ActionResult<AddressCreatedResponse>> CreateAddress([FromBody] CreateAddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Line1))
            return BadRequest(new { message = "Line1 is required." });

        var userId = GetCurrentUserId();

        using var conn = CreateConnection();

        // Get city with full hierarchy for geocoding
        var cityInfo = await conn.QuerySingleOrDefaultAsync<CityFullInfo>(
            @"SELECT c.Id, c.Name AS CityName, c.Latitude, c.Longitude,
                     p.Name AS ProvinceStateName, p.Code AS ProvinceStateCode,
                     co.Name AS CountryName, co.Code2 AS CountryCode
              FROM Cities c
              JOIN ProvinceStates p ON c.ProvinceStateId = p.Id
              JOIN Countries co ON p.CountryId = co.Id
              WHERE c.Id = @Id AND c.IsActive = 1",
            new { Id = request.CityId });

        if (cityInfo == null)
            return BadRequest(new { message = "City not found." });

        // Check for existing duplicate address (same city, line1, postal code)
        var normalizedLine1 = request.Line1.Trim();
        var normalizedPostal = request.PostalCode?.Trim();

        var existing = await conn.QuerySingleOrDefaultAsync<ExistingAddressInfo>(
            @"SELECT Id, Latitude, Longitude, IsVerified
              FROM Addresses
              WHERE CityId = @CityId
                AND LOWER(LTRIM(RTRIM(Line1))) = LOWER(@Line1)
                AND ((@PostalCode IS NULL AND PostalCode IS NULL) OR LOWER(LTRIM(RTRIM(PostalCode))) = LOWER(@PostalCode))",
            new { request.CityId, Line1 = normalizedLine1, PostalCode = normalizedPostal });

        if (existing != null)
        {
            // Return existing address - use its GPS or fallback to city
            var existingLat = existing.Latitude ?? cityInfo.Latitude;
            var existingLng = existing.Longitude ?? cityInfo.Longitude;

            _logger.LogDebug("Returning existing address {AddressId} (duplicate detected)", existing.Id);

            return Ok(new AddressCreatedResponse
            {
                Id = existing.Id,
                Latitude = existingLat,
                Longitude = existingLng,
                IsVerified = existing.IsVerified,
                GpsSource = existing.Latitude.HasValue ? "address" : (cityInfo.Latitude.HasValue ? "city" : "none"),
                IsExisting = true
            });
        }

        // Determine GPS coordinates
        decimal? latitude = request.Latitude;
        decimal? longitude = request.Longitude;
        string gpsSource = "none";
        bool isVerified = false;

        if (latitude.HasValue && longitude.HasValue)
        {
            // GPS provided in request
            gpsSource = "address";
            isVerified = true;
        }
        else if (_geocodingService.IsEnabled)
        {
            // Try geocoding
            var geocodeRequest = new GeocodingRequest
            {
                Line1 = normalizedLine1,
                Line2 = request.Line2?.Trim(),
                City = cityInfo.CityName,
                StateProvince = cityInfo.ProvinceStateName,
                PostalCode = normalizedPostal,
                Country = cityInfo.CountryName,
                CountryCode = cityInfo.CountryCode
            };

            var geocodeResult = await _geocodingService.GeocodeAsync(geocodeRequest);

            if (geocodeResult.Success && geocodeResult.Latitude.HasValue && geocodeResult.Longitude.HasValue)
            {
                latitude = geocodeResult.Latitude;
                longitude = geocodeResult.Longitude;
                gpsSource = $"geocoded:{geocodeResult.Provider}";
                isVerified = true;
                _logger.LogInformation("Address geocoded via {Provider}: ({Lat}, {Lng})",
                    geocodeResult.Provider, latitude, longitude);
            }
            else
            {
                _logger.LogDebug("Geocoding failed: {Error}, falling back to city GPS", geocodeResult.Error);
            }
        }

        // Fall back to city GPS if still no coordinates
        if (!latitude.HasValue || !longitude.HasValue)
        {
            latitude = cityInfo.Latitude;
            longitude = cityInfo.Longitude;
            gpsSource = cityInfo.Latitude.HasValue ? "city" : "none";
        }

        // Create new address
        var newId = await conn.QuerySingleAsync<int>(
            @"INSERT INTO Addresses (CityId, Line1, Line2, PostalCode, Latitude, Longitude, IsVerified, CreatedByUserId)
              OUTPUT INSERTED.Id
              VALUES (@CityId, @Line1, @Line2, @PostalCode, @Latitude, @Longitude, @IsVerified, @CreatedByUserId)",
            new
            {
                request.CityId,
                Line1 = normalizedLine1,
                Line2 = request.Line2?.Trim(),
                PostalCode = normalizedPostal,
                Latitude = latitude,
                Longitude = longitude,
                IsVerified = isVerified,
                CreatedByUserId = userId
            });

        _logger.LogInformation("Address {AddressId} created by user {UserId}, GPS source: {GpsSource}",
            newId, userId, gpsSource);

        return Ok(new AddressCreatedResponse
        {
            Id = newId,
            Latitude = latitude,
            Longitude = longitude,
            IsVerified = isVerified,
            GpsSource = gpsSource,
            IsExisting = false
        });
    }

    /// <summary>
    /// Update an existing address
    /// </summary>
    [HttpPut("{id:int}")]
    [ApiKeyAuthorize(ApiScopes.AddressesWrite, AllowJwt = true)]
    public async Task<ActionResult<AddressCreatedResponse>> UpdateAddress(int id, [FromBody] UpdateAddressRequest request)
    {
        using var conn = CreateConnection();

        // Verify address exists
        var exists = await conn.ExecuteScalarAsync<bool>(
            "SELECT CASE WHEN EXISTS(SELECT 1 FROM Addresses WHERE Id = @Id) THEN 1 ELSE 0 END",
            new { Id = id });

        if (!exists)
            return NotFound(new { message = "Address not found." });

        // If changing city, verify new city exists
        if (request.CityId.HasValue)
        {
            var cityExists = await conn.ExecuteScalarAsync<bool>(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM Cities WHERE Id = @Id AND IsActive = 1) THEN 1 ELSE 0 END",
                new { Id = request.CityId.Value });

            if (!cityExists)
                return BadRequest(new { message = "City not found." });
        }

        // Build dynamic update
        var updates = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        if (request.CityId.HasValue) { updates.Add("CityId = @CityId"); parameters.Add("CityId", request.CityId.Value); }
        if (request.Line1 != null) { updates.Add("Line1 = @Line1"); parameters.Add("Line1", request.Line1); }
        if (request.Line2 != null) { updates.Add("Line2 = @Line2"); parameters.Add("Line2", request.Line2 == "" ? null : request.Line2); }
        if (request.PostalCode != null) { updates.Add("PostalCode = @PostalCode"); parameters.Add("PostalCode", request.PostalCode == "" ? null : request.PostalCode); }
        if (request.Latitude.HasValue) { updates.Add("Latitude = @Latitude"); parameters.Add("Latitude", request.Latitude); }
        if (request.Longitude.HasValue) { updates.Add("Longitude = @Longitude"); parameters.Add("Longitude", request.Longitude); }
        if (request.IsVerified.HasValue) { updates.Add("IsVerified = @IsVerified"); parameters.Add("IsVerified", request.IsVerified.Value); }

        if (updates.Count == 0)
            return BadRequest(new { message = "No fields to update." });

        updates.Add("UpdatedAt = GETUTCDATE()");

        await conn.ExecuteAsync(
            $"UPDATE Addresses SET {string.Join(", ", updates)} WHERE Id = @Id",
            parameters);

        // Get updated address GPS info
        var address = await conn.QuerySingleAsync<AddressGpsInfo>(
            @"SELECT a.Id, a.Latitude, a.Longitude, a.IsVerified,
                     c.Latitude AS CityLatitude, c.Longitude AS CityLongitude
              FROM Addresses a
              JOIN Cities c ON a.CityId = c.Id
              WHERE a.Id = @Id", new { Id = id });

        var responseLatitude = address.Latitude ?? address.CityLatitude;
        var responseLongitude = address.Longitude ?? address.CityLongitude;

        _logger.LogInformation("Address {AddressId} updated", id);

        return Ok(new AddressCreatedResponse
        {
            Id = id,
            Latitude = responseLatitude,
            Longitude = responseLongitude,
            IsVerified = address.IsVerified,
            GpsSource = address.Latitude.HasValue ? "address" : (address.CityLatitude.HasValue ? "city" : "none")
        });
    }

    /// <summary>
    /// Update only GPS coordinates for an address
    /// </summary>
    [HttpPut("{id:int}/gps")]
    [ApiKeyAuthorize(ApiScopes.AddressesWrite, AllowJwt = true)]
    public async Task<ActionResult<AddressCreatedResponse>> UpdateAddressGps(int id, [FromBody] UpdateAddressGpsRequest request)
    {
        using var conn = CreateConnection();

        var rowsAffected = await conn.ExecuteAsync(
            @"UPDATE Addresses SET
                Latitude = @Latitude,
                Longitude = @Longitude,
                IsVerified = @IsVerified,
                UpdatedAt = GETUTCDATE()
              WHERE Id = @Id",
            new { Id = id, request.Latitude, request.Longitude, IsVerified = request.IsVerified ?? true });

        if (rowsAffected == 0)
            return NotFound(new { message = "Address not found." });

        _logger.LogInformation("Address {AddressId} GPS updated to ({Lat}, {Lng})",
            id, request.Latitude, request.Longitude);

        return Ok(new AddressCreatedResponse
        {
            Id = id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsVerified = request.IsVerified ?? true,
            GpsSource = "address"
        });
    }

    /// <summary>
    /// Lookup existing address to avoid duplicates
    /// </summary>
    [HttpGet("lookup")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AddressLookupResponse>>> LookupAddress(
        [FromQuery] int cityId,
        [FromQuery] string line1,
        [FromQuery] string? postalCode = null)
    {
        if (string.IsNullOrWhiteSpace(line1))
            return BadRequest(new { message = "line1 is required." });

        using var conn = CreateConnection();
        var addresses = await conn.QueryAsync<AddressLookupResponse>(
            @"SELECT a.Id, a.Line1, a.Line2, a.PostalCode,
                     a.Latitude, a.Longitude, a.IsVerified
              FROM Addresses a
              WHERE a.CityId = @CityId
                AND a.Line1 LIKE @Line1
                AND (@PostalCode IS NULL OR a.PostalCode = @PostalCode)",
            new { CityId = cityId, Line1 = $"{line1}%", PostalCode = postalCode });

        return Ok(addresses.ToList());
    }

    /// <summary>
    /// Quick GPS lookup for an address (for LBS caching)
    /// </summary>
    [HttpGet("{id:int}/gps")]
    [AllowAnonymous]
    public async Task<ActionResult<AddressGpsResponse>> GetAddressGps(int id)
    {
        using var conn = CreateConnection();
        var gps = await conn.QuerySingleOrDefaultAsync<AddressGpsResponse>(
            @"SELECT a.Id,
                     COALESCE(a.Latitude, c.Latitude) AS Latitude,
                     COALESCE(a.Longitude, c.Longitude) AS Longitude,
                     a.IsVerified,
                     CASE WHEN a.Latitude IS NOT NULL THEN 'address'
                          WHEN c.Latitude IS NOT NULL THEN 'city'
                          ELSE 'none' END AS GpsSource
              FROM Addresses a
              JOIN Cities c ON a.CityId = c.Id
              WHERE a.Id = @Id", new { Id = id });

        if (gps == null)
            return NotFound(new { message = "Address not found." });

        return Ok(gps);
    }

    /// <summary>
    /// Batch GPS lookup for multiple addresses
    /// </summary>
    [HttpPost("gps/batch")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AddressGpsResponse>>> GetAddressGpsBatch([FromBody] List<int> addressIds)
    {
        if (addressIds == null || addressIds.Count == 0)
            return BadRequest(new { message = "Address IDs required." });

        if (addressIds.Count > 100)
            return BadRequest(new { message = "Maximum 100 addresses per batch." });

        using var conn = CreateConnection();
        var results = await conn.QueryAsync<AddressGpsResponse>(
            @"SELECT a.Id,
                     COALESCE(a.Latitude, c.Latitude) AS Latitude,
                     COALESCE(a.Longitude, c.Longitude) AS Longitude,
                     a.IsVerified,
                     CASE WHEN a.Latitude IS NOT NULL THEN 'address'
                          WHEN c.Latitude IS NOT NULL THEN 'city'
                          ELSE 'none' END AS GpsSource
              FROM Addresses a
              JOIN Cities c ON a.CityId = c.Id
              WHERE a.Id IN @Ids",
            new { Ids = addressIds });

        return Ok(results.ToList());
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

#region DTOs

public class AddressDetailResponse
{
    public int Id { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // City info
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public decimal? CityLatitude { get; set; }
    public decimal? CityLongitude { get; set; }

    // Province info
    public int ProvinceStateId { get; set; }
    public string ProvinceStateName { get; set; } = string.Empty;
    public string ProvinceStateCode { get; set; } = string.Empty;

    // Country info
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class CreateAddressRequest
{
    public int CityId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class UpdateAddressRequest
{
    public int? CityId { get; set; }
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool? IsVerified { get; set; }
}

public class UpdateAddressGpsRequest
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool? IsVerified { get; set; }
}

public class AddressCreatedResponse
{
    public int Id { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsVerified { get; set; }
    public string GpsSource { get; set; } = "none";  // "address", "city", or "none"
    /// <summary>
    /// True if an existing duplicate address was found and returned instead of creating new
    /// </summary>
    public bool IsExisting { get; set; } = false;
}

public class AddressLookupResponse
{
    public int Id { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsVerified { get; set; }
}

public class AddressGpsResponse
{
    public int Id { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsVerified { get; set; }
    public string GpsSource { get; set; } = "none";
}

// Internal helper classes
internal class CityGpsInfo
{
    public int Id { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

internal class CityFullInfo
{
    public int Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string ProvinceStateName { get; set; } = string.Empty;
    public string ProvinceStateCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

internal class ExistingAddressInfo
{
    public int Id { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsVerified { get; set; }
}

internal class AddressGpsInfo
{
    public int Id { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsVerified { get; set; }
    public decimal? CityLatitude { get; set; }
    public decimal? CityLongitude { get; set; }
}

#endregion
