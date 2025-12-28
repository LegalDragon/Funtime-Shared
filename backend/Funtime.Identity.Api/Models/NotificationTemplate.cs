namespace Funtime.Identity.Api.Models;

/// <summary>
/// Email template matching FXNotification.EmailTemplates table
/// </summary>
public class EmailTemplateRow
{
    public int ET_ID { get; set; }
    public string? ET_Code { get; set; }
    public string? Lang_Code { get; set; } = "en";
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? App_Code { get; set; }
}
