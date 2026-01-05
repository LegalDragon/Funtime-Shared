using System.Text.Json.Serialization;

namespace Funtime.Identity.Api.Models;

/// <summary>
/// Email template matching FXNotification.EmailTemplates table
/// </summary>
public class EmailTemplateRow
{
    [JsonPropertyName("eT_ID")]
    public int ET_ID { get; set; }

    [JsonPropertyName("eT_Code")]
    public string? ET_Code { get; set; }

    [JsonPropertyName("app_ID")]
    public int? App_ID { get; set; } = 0;

    [JsonPropertyName("lang_Code")]
    public string? Lang_Code { get; set; } = "en";

    [JsonPropertyName("sendFrom")]
    public string? SendFrom { get; set; }

    [JsonPropertyName("sendTo")]
    public string? SendTo { get; set; }

    [JsonPropertyName("bcc")]
    public string? BCC { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("app_Code")]
    public string? App_Code { get; set; }
}
