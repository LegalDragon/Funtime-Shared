namespace Funtime.Identity.Api.Services;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message);
}
