using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Services;

public interface IJwtService
{
    Task<string> GenerateTokenAsync(User user);
    string GenerateToken(User user);
    (bool isValid, int? userId, string? email, string? phoneNumber, string? systemRole, List<string>? sites) ValidateToken(string token);
}
