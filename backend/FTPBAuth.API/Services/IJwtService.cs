using FTPBAuth.API.Models;

namespace FTPBAuth.API.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    (bool isValid, int? userId, string? email, string? phoneNumber) ValidateToken(string token);
}
