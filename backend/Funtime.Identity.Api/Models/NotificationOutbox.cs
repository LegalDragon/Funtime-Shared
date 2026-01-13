namespace Funtime.Identity.Api.Models;

/// <summary>
/// Outbox row matching FXNotification.EmailOutbox table
/// </summary>
public class OutboxRow
{
    public int Id { get; set; }
    public int? TaskId { get; set; }
    public string? TaskCode { get; set; }
    public string? TaskStatus { get; set; }
    public string? TemplateCode { get; set; }
    public string? LangCode { get; set; }
    public string? EmailFrom { get; set; }
    public string? EmailFromName { get; set; }
    public int? MailPriority { get; set; }
    public string? ObjectId { get; set; }
    public string? ToList { get; set; }
    public string? CcList { get; set; }
    public string? BccList { get; set; }
    public string? Subject { get; set; }
    public string? BodyHtml { get; set; }
    public string? BodyJson { get; set; }
    public string? DetailJson { get; set; }
    public int Attempts { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
}
