namespace FTPBAuth.API.Services;

public interface IOtpService
{
    Task<(bool success, string message)> SendOtpAsync(string phoneNumber);
    Task<(bool success, string message)> VerifyOtpAsync(string phoneNumber, string code);
    Task<bool> IsRateLimitedAsync(string phoneNumber);
}
