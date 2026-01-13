namespace Funtime.Identity.Api.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body);
}
