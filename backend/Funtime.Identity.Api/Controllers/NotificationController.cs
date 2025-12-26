using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("admin/notifications")]
[Authorize(Roles = "SU")]
public class NotificationController : ControllerBase
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(NotificationDbContext context, ILogger<NotificationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Mail Profiles

    [HttpGet("profiles")]
    public async Task<ActionResult<List<MailProfile>>> GetMailProfiles([FromQuery] string? siteKey = null)
    {
        try
        {
            var query = _context.MailProfiles.AsQueryable();
            if (!string.IsNullOrEmpty(siteKey))
                query = query.Where(p => p.SiteKey == siteKey || p.SiteKey == null);

            return await query.OrderBy(p => p.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mail profiles - database may not be configured");
            return StatusCode(500, new { message = "Notification database not available. Please run the migration on fxEmail database." });
        }
    }

    [HttpGet("profiles/{id}")]
    public async Task<ActionResult<MailProfile>> GetMailProfile(int id)
    {
        var profile = await _context.MailProfiles.FindAsync(id);
        if (profile == null) return NotFound();
        return profile;
    }

    [HttpPost("profiles")]
    public async Task<ActionResult<MailProfile>> CreateMailProfile([FromBody] MailProfile profile)
    {
        profile.CreatedAt = DateTime.UtcNow;
        _context.MailProfiles.Add(profile);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Mail profile {Name} created", profile.Name);
        return CreatedAtAction(nameof(GetMailProfile), new { id = profile.Id }, profile);
    }

    [HttpPut("profiles/{id}")]
    public async Task<ActionResult<MailProfile>> UpdateMailProfile(int id, [FromBody] MailProfile updates)
    {
        var profile = await _context.MailProfiles.FindAsync(id);
        if (profile == null) return NotFound();

        profile.Name = updates.Name;
        profile.SmtpHost = updates.SmtpHost;
        profile.SmtpPort = updates.SmtpPort;
        profile.Username = updates.Username;
        if (!string.IsNullOrEmpty(updates.Password))
            profile.Password = updates.Password;
        profile.FromEmail = updates.FromEmail;
        profile.FromName = updates.FromName;
        profile.SecurityMode = updates.SecurityMode;
        profile.IsActive = updates.IsActive;
        profile.SiteKey = updates.SiteKey;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Mail profile {Id} updated", id);
        return profile;
    }

    [HttpDelete("profiles/{id}")]
    public async Task<ActionResult> DeleteMailProfile(int id)
    {
        var profile = await _context.MailProfiles.FindAsync(id);
        if (profile == null) return NotFound();

        _context.MailProfiles.Remove(profile);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Mail profile {Id} deleted", id);
        return Ok(new { message = "Profile deleted" });
    }

    #endregion

    #region Templates

    [HttpGet("templates")]
    public async Task<ActionResult<List<NotificationTemplate>>> GetTemplates(
        [FromQuery] string? siteKey = null,
        [FromQuery] string? type = null)
    {
        var query = _context.NotificationTemplates.AsQueryable();
        if (!string.IsNullOrEmpty(siteKey))
            query = query.Where(t => t.SiteKey == siteKey || t.SiteKey == null);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.Type == type);

        return await query.OrderBy(t => t.Code).ThenBy(t => t.Language).ToListAsync();
    }

    [HttpGet("templates/{id}")]
    public async Task<ActionResult<NotificationTemplate>> GetTemplate(int id)
    {
        var template = await _context.NotificationTemplates.FindAsync(id);
        if (template == null) return NotFound();
        return template;
    }

    [HttpPost("templates")]
    public async Task<ActionResult<NotificationTemplate>> CreateTemplate([FromBody] NotificationTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Template {Code} created", template.Code);
        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    [HttpPut("templates/{id}")]
    public async Task<ActionResult<NotificationTemplate>> UpdateTemplate(int id, [FromBody] NotificationTemplate updates)
    {
        var template = await _context.NotificationTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.Code = updates.Code;
        template.Name = updates.Name;
        template.Type = updates.Type;
        template.Language = updates.Language;
        template.Subject = updates.Subject;
        template.Body = updates.Body;
        template.BodyText = updates.BodyText;
        template.SiteKey = updates.SiteKey;
        template.IsActive = updates.IsActive;
        template.Description = updates.Description;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Template {Id} updated", id);
        return template;
    }

    [HttpDelete("templates/{id}")]
    public async Task<ActionResult> DeleteTemplate(int id)
    {
        var template = await _context.NotificationTemplates.FindAsync(id);
        if (template == null) return NotFound();

        _context.NotificationTemplates.Remove(template);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Template {Id} deleted", id);
        return Ok(new { message = "Template deleted" });
    }

    #endregion

    #region Tasks

    [HttpGet("tasks")]
    public async Task<ActionResult<List<NotificationTaskDto>>> GetTasks(
        [FromQuery] string? siteKey = null,
        [FromQuery] string? status = null)
    {
        var query = _context.NotificationTasks
            .Include(t => t.MailProfile)
            .Include(t => t.Template)
            .AsQueryable();

        if (!string.IsNullOrEmpty(siteKey))
            query = query.Where(t => t.SiteKey == siteKey || t.SiteKey == null);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var tasks = await query.OrderBy(t => t.Code).ToListAsync();
        return tasks.Select(t => new NotificationTaskDto
        {
            Id = t.Id,
            Code = t.Code,
            Name = t.Name,
            Type = t.Type,
            Status = t.Status,
            Priority = t.Priority,
            MailProfileId = t.MailProfileId,
            MailProfileName = t.MailProfile?.Name,
            TemplateId = t.TemplateId,
            TemplateCode = t.Template?.Code,
            SiteKey = t.SiteKey,
            DefaultRecipients = t.DefaultRecipients,
            CcRecipients = t.CcRecipients,
            BccRecipients = t.BccRecipients,
            TestEmail = t.TestEmail,
            MaxRetries = t.MaxRetries,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    [HttpGet("tasks/{id}")]
    public async Task<ActionResult<NotificationTask>> GetTask(int id)
    {
        var task = await _context.NotificationTasks
            .Include(t => t.MailProfile)
            .Include(t => t.Template)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return NotFound();
        return task;
    }

    [HttpPost("tasks")]
    public async Task<ActionResult<NotificationTask>> CreateTask([FromBody] NotificationTask task)
    {
        task.CreatedAt = DateTime.UtcNow;
        _context.NotificationTasks.Add(task);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Task {Code} created", task.Code);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("tasks/{id}")]
    public async Task<ActionResult<NotificationTask>> UpdateTask(int id, [FromBody] NotificationTask updates)
    {
        var task = await _context.NotificationTasks.FindAsync(id);
        if (task == null) return NotFound();

        task.Code = updates.Code;
        task.Name = updates.Name;
        task.Type = updates.Type;
        task.Status = updates.Status;
        task.Priority = updates.Priority;
        task.MailProfileId = updates.MailProfileId;
        task.TemplateId = updates.TemplateId;
        task.SiteKey = updates.SiteKey;
        task.DefaultRecipients = updates.DefaultRecipients;
        task.CcRecipients = updates.CcRecipients;
        task.BccRecipients = updates.BccRecipients;
        task.TestEmail = updates.TestEmail;
        task.MaxRetries = updates.MaxRetries;
        task.Description = updates.Description;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Task {Id} updated", id);
        return task;
    }

    [HttpDelete("tasks/{id}")]
    public async Task<ActionResult> DeleteTask(int id)
    {
        var task = await _context.NotificationTasks.FindAsync(id);
        if (task == null) return NotFound();

        _context.NotificationTasks.Remove(task);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Task {Id} deleted", id);
        return Ok(new { message = "Task deleted" });
    }

    #endregion

    #region Outbox

    [HttpGet("outbox")]
    public async Task<ActionResult<OutboxListResponse>> GetOutbox(
        [FromQuery] string? status = null,
        [FromQuery] string? siteKey = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.NotificationOutbox.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        if (!string.IsNullOrEmpty(siteKey))
            query = query.Where(o => o.SiteKey == siteKey);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new OutboxListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    [HttpPost("outbox/{id}/retry")]
    public async Task<ActionResult> RetryOutbox(int id)
    {
        var item = await _context.NotificationOutbox.FindAsync(id);
        if (item == null) return NotFound();

        item.Status = "Pending";
        item.Attempts = 0;
        item.LastError = null;
        item.NextRetryAt = null;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Outbox item {Id} queued for retry", id);
        return Ok(new { message = "Queued for retry" });
    }

    [HttpDelete("outbox/{id}")]
    public async Task<ActionResult> DeleteOutbox(int id)
    {
        var item = await _context.NotificationOutbox.FindAsync(id);
        if (item == null) return NotFound();

        _context.NotificationOutbox.Remove(item);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Outbox item {Id} deleted", id);
        return Ok(new { message = "Deleted" });
    }

    [HttpPost("outbox/clear-failed")]
    public async Task<ActionResult> ClearFailedOutbox()
    {
        var failed = await _context.NotificationOutbox
            .Where(o => o.Status == "Failed")
            .ToListAsync();

        _context.NotificationOutbox.RemoveRange(failed);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleared {Count} failed outbox items", failed.Count);
        return Ok(new { message = $"Cleared {failed.Count} failed items" });
    }

    #endregion

    #region History

    [HttpGet("history")]
    public async Task<ActionResult<HistoryListResponse>> GetHistory(
        [FromQuery] string? status = null,
        [FromQuery] string? siteKey = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.NotificationHistory.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(h => h.Status == status);
        if (!string.IsNullOrEmpty(siteKey))
            query = query.Where(h => h.SiteKey == siteKey);
        if (fromDate.HasValue)
            query = query.Where(h => h.SentAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(h => h.SentAt <= toDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new HistoryListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    #endregion

    #region Stats

    [HttpGet("stats")]
    public async Task<ActionResult<NotificationStats>> GetStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);

            return new NotificationStats
            {
                TotalProfiles = await _context.MailProfiles.CountAsync(),
                ActiveProfiles = await _context.MailProfiles.CountAsync(p => p.IsActive),
                TotalTemplates = await _context.NotificationTemplates.CountAsync(),
                TotalTasks = await _context.NotificationTasks.CountAsync(),
                ActiveTasks = await _context.NotificationTasks.CountAsync(t => t.Status == "Active"),
                PendingMessages = await _context.NotificationOutbox.CountAsync(o => o.Status == "Pending"),
                FailedMessages = await _context.NotificationOutbox.CountAsync(o => o.Status == "Failed"),
                SentToday = await _context.NotificationHistory.CountAsync(h => h.SentAt >= today),
                SentThisWeek = await _context.NotificationHistory.CountAsync(h => h.SentAt >= thisWeek)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats - database may not be configured");
            return StatusCode(500, new { message = "Notification database not available. Please run the migration on fxEmail database." });
        }
    }

    #endregion
}

#region DTOs

public class NotificationTaskDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int? MailProfileId { get; set; }
    public string? MailProfileName { get; set; }
    public int? TemplateId { get; set; }
    public string? TemplateCode { get; set; }
    public string? SiteKey { get; set; }
    public string? DefaultRecipients { get; set; }
    public string? CcRecipients { get; set; }
    public string? BccRecipients { get; set; }
    public string? TestEmail { get; set; }
    public int MaxRetries { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OutboxListResponse
{
    public List<NotificationOutbox> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class HistoryListResponse
{
    public List<NotificationHistory> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class NotificationStats
{
    public int TotalProfiles { get; set; }
    public int ActiveProfiles { get; set; }
    public int TotalTemplates { get; set; }
    public int TotalTasks { get; set; }
    public int ActiveTasks { get; set; }
    public int PendingMessages { get; set; }
    public int FailedMessages { get; set; }
    public int SentToday { get; set; }
    public int SentThisWeek { get; set; }
}

#endregion
