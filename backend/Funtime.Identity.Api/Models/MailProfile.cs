namespace Funtime.Identity.Api.Models;

/// <summary>
/// SMTP mail profile matching FXNotification.MailProfiles table
/// </summary>
public class MailProfileRow
{
    public int ProfileId { get; set; }
    public string? ProfileCode { get; set; }
    public int? App_ID { get; set; }
    public string? FromName { get; set; }
    public string? FromEmail { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? AuthUser { get; set; }
    public string? AuthSecretRef { get; set; }
    public string? SecurityMode { get; set; } = "StartTlsWhenAvailable";
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Application registration matching FXNotification.Apps table
/// </summary>
public class AppRow
{
    public int App_ID { get; set; }
    public string? App_Code { get; set; }
    public string? Descr { get; set; }
    public int? ProfileID { get; set; }
}
