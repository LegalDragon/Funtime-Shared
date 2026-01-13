using System.Text.Json.Serialization;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// History row matching FXNotification.EmailHistory table
/// </summary>
public class HistoryRow
{
    [JsonPropertyName("iD")]
    public int ID { get; set; }
    public int? TaskId { get; set; }
    public string? TaskCode { get; set; }
    public string? ToList { get; set; }
    public string? CcList { get; set; }
    public string? BccList { get; set; }
    public string? Subject { get; set; }
    public string? Status { get; set; }
    public int Attempts { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Audit row for history details
/// </summary>
public class AuditRow
{
    public int AuditId { get; set; }
    public int? EmailId { get; set; }
    public string? Action { get; set; }
    public string? Details { get; set; }
    public DateTime? CreatedAt { get; set; }
}
