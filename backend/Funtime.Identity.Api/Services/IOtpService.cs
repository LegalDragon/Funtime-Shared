namespace Funtime.Identity.Api.Services;

public interface IOtpService
{
    Task<(bool success, string message)> SendOtpAsync(string identifier);
    Task<(bool success, string message, int? userId)> VerifyOtpAsync(string identifier, string code, bool markAsUsed = true);
    Task<bool> IsRateLimitedAsync(string identifier);
}
