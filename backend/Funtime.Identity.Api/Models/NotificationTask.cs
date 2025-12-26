namespace Funtime.Identity.Api.Models;

/// <summary>
/// Task configuration matching FXNotification.Tasks table
/// </summary>
public class TaskRow
{
    public int Task_ID { get; set; }
    public string? TaskCode { get; set; }
    public string? TaskType { get; set; } = "Email";
    public int? App_ID { get; set; }
    public int? ProfileID { get; set; }
    public int? TemplateID { get; set; }
    public string? Status { get; set; } = "Active";
    public string? TestMailTo { get; set; }
    public string? LangCode { get; set; } = "en";
    public string? MailFromName { get; set; }
    public string? MailFrom { get; set; }
    public string? MailTo { get; set; }
    public string? MailCC { get; set; }
    public string? MailBCC { get; set; }
    public string? AttachmentProcName { get; set; }
}
