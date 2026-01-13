using System.ComponentModel.DataAnnotations;

namespace Funtime.Identity.Api.Models;

public class Setting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }
}

public static class SettingKeys
{
    public const string TermsOfService = "terms_of_service";
    public const string PrivacyPolicy = "privacy_policy";
}
