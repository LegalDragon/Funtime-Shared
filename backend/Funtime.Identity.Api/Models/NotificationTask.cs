using System.Text.Json.Serialization;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Task configuration matching FXNotification.Tasks table
/// </summary>
public class TaskRow
{
    [JsonPropertyName("task_ID")]
    public int Task_ID { get; set; }

    [JsonPropertyName("taskCode")]
    public string? TaskCode { get; set; }

    [JsonPropertyName("taskType")]
    public string? TaskType { get; set; } = "Email";

    [JsonPropertyName("app_ID")]
    public int? App_ID { get; set; }

    [JsonPropertyName("profileID")]
    public int? ProfileID { get; set; }

    [JsonPropertyName("templateID")]
    public int? TemplateID { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; } = "Active";

    [JsonPropertyName("testMailTo")]
    public string? TestMailTo { get; set; }

    [JsonPropertyName("langCode")]
    public string? LangCode { get; set; } = "en";

    [JsonPropertyName("mailFromName")]
    public string? MailFromName { get; set; }

    [JsonPropertyName("mailFrom")]
    public string? MailFrom { get; set; }

    [JsonPropertyName("mailTo")]
    public string? MailTo { get; set; }

    [JsonPropertyName("mailCC")]
    public string? MailCC { get; set; }

    [JsonPropertyName("mailBCC")]
    public string? MailBCC { get; set; }

    [JsonPropertyName("attachmentProcName")]
    public string? AttachmentProcName { get; set; }
}
