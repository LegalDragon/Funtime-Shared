using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public JwtService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<string> GenerateTokenAsync(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
        }

        if (!string.IsNullOrEmpty(user.SystemRole))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.SystemRole));
        }

        // Fetch user's active sites
        List<string> userSites;

        // SU users have access to ALL sites
        if (user.SystemRole == "SU")
        {
            userSites = await _context.Sites
                .Where(s => s.IsActive)
                .Select(s => s.Key)
                .ToListAsync();
        }
        else
        {
            userSites = await _context.UserSites
                .Where(s => s.UserId == user.Id && s.IsActive)
                .Select(s => s.SiteKey)
                .ToListAsync();
        }

        // Always include sites claim (even if empty array)
        claims.Add(new Claim("sites", JsonSerializer.Serialize(userSites)));

        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Legacy sync method for backward compatibility (does not include sites)
    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
        }

        if (!string.IsNullOrEmpty(user.SystemRole))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.SystemRole));
        }

        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (bool isValid, int? userId, string? email, string? phoneNumber, string? systemRole, List<string>? sites) ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return (false, null, null, null, null, null);
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            var phoneClaim = principal.FindFirst(ClaimTypes.MobilePhone)?.Value;
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
            var sitesClaim = principal.FindFirst("sites")?.Value;

            List<string>? sites = null;
            if (!string.IsNullOrEmpty(sitesClaim))
            {
                sites = JsonSerializer.Deserialize<List<string>>(sitesClaim);
            }

            if (int.TryParse(userIdClaim, out var userId))
            {
                return (true, userId, emailClaim, phoneClaim, roleClaim, sites);
            }

            return (false, null, null, null, null, null);
        }
        catch
        {
            return (false, null, null, null, null, null);
        }
    }
}
