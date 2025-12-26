using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Funtime.Identity.Api.Models;

namespace Funtime.Identity.Api.Controllers;

[ApiController]
[Route("admin/notifications")]
[Authorize(Roles = "SU")]
public class NotificationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationController> _logger;
    private readonly string _connectionString;

    public NotificationController(IConfiguration configuration, ILogger<NotificationController> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("NotificationConnection")
            ?? throw new InvalidOperationException("NotificationConnection not configured");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    #region Mail Profiles

    [HttpGet("profiles")]
    public async Task<ActionResult<List<MailProfileRow>>> GetMailProfiles()
    {
        try
        {
            using var conn = CreateConnection();
            var profiles = await conn.QueryAsync<MailProfileRow>("exec dbo.csp_Profiles_Get");
            return profiles.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mail profiles");
            return StatusCode(500, new { message = $"Failed to get mail profiles: {ex.Message}" });
        }
    }

    [HttpPost("profiles")]
    public async Task<ActionResult<MailProfileRow>> CreateMailProfile([FromBody] MailProfileRow profile)
    {
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QuerySingleOrDefaultAsync<MailProfileRow>(
                "exec dbo.csp_MailProfile_AddNew @ProfileCode, @App_ID, @FromName, @FromEmail, @SmtpHost, @SmtpPort, @AuthUser, @AuthSecretRef, @SecurityMode, @IsActive",
                new
                {
                    profile.ProfileCode,
                    profile.App_ID,
                    profile.FromName,
                    profile.FromEmail,
                    profile.SmtpHost,
                    profile.SmtpPort,
                    profile.AuthUser,
                    profile.AuthSecretRef,
                    profile.SecurityMode,
                    profile.IsActive
                });
            _logger.LogInformation("Mail profile {ProfileCode} created", profile.ProfileCode);
            return result ?? profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create mail profile");
            return StatusCode(500, new { message = "Failed to create mail profile" });
        }
    }

    [HttpPut("profiles/{id}")]
    public async Task<ActionResult<MailProfileRow>> UpdateMailProfile(int id, [FromBody] MailProfileRow profile)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                "exec dbo.csp_MailProfile_Update @ProfileId, @ProfileCode, @App_ID, @FromName, @FromEmail, @SmtpHost, @SmtpPort, @AuthUser, @AuthSecretRef, @SecurityMode, @IsActive",
                new
                {
                    ProfileId = id,
                    profile.ProfileCode,
                    profile.App_ID,
                    profile.FromName,
                    profile.FromEmail,
                    profile.SmtpHost,
                    profile.SmtpPort,
                    profile.AuthUser,
                    profile.AuthSecretRef,
                    profile.SecurityMode,
                    profile.IsActive
                });
            _logger.LogInformation("Mail profile {Id} updated", id);
            profile.ProfileId = id;
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update mail profile {Id}", id);
            return StatusCode(500, new { message = "Failed to update mail profile" });
        }
    }

    #endregion

    #region Applications

    [HttpGet("apps")]
    public async Task<ActionResult<List<AppRow>>> GetApps()
    {
        try
        {
            using var conn = CreateConnection();
            var apps = await conn.QueryAsync<AppRow>("exec dbo.csp_Get_Apps");
            return apps.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get apps");
            return StatusCode(500, new { message = "Failed to get applications" });
        }
    }

    [HttpPost("apps")]
    public async Task<ActionResult<AppRow>> CreateApp([FromBody] AppRow app)
    {
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QuerySingleOrDefaultAsync<AppRow>(
                "exec dbo.csp_Apps_Add @App_Code, @Descr, @ProfileID",
                new { app.App_Code, app.Descr, app.ProfileID });
            _logger.LogInformation("App {AppCode} created", app.App_Code);
            return result ?? app;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create app");
            return StatusCode(500, new { message = "Failed to create application" });
        }
    }

    [HttpPut("apps/{id}")]
    public async Task<ActionResult<AppRow>> UpdateApp(int id, [FromBody] AppRow app)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                "exec dbo.csp_Apps_Update @App_ID, @App_Code, @Descr, @ProfileID",
                new { App_ID = id, app.App_Code, app.Descr, app.ProfileID });
            _logger.LogInformation("App {Id} updated", id);
            app.App_ID = id;
            return app;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update app {Id}", id);
            return StatusCode(500, new { message = "Failed to update application" });
        }
    }

    #endregion

    #region Templates

    [HttpGet("templates")]
    public async Task<ActionResult<List<EmailTemplateRow>>> GetTemplates([FromQuery] int? appId = null)
    {
        try
        {
            using var conn = CreateConnection();
            var templates = await conn.QueryAsync<EmailTemplateRow>(
                "exec dbo.csp_Get_Email_Templates @App_ID",
                new { App_ID = appId });
            return templates.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get templates");
            return StatusCode(500, new { message = "Failed to get templates" });
        }
    }

    [HttpPost("templates")]
    public async Task<ActionResult<EmailTemplateRow>> CreateTemplate([FromBody] EmailTemplateRow template)
    {
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QuerySingleOrDefaultAsync<EmailTemplateRow>(
                "exec dbo.csp_Email_Templates_AddNew @ET_Code, @Subject, @Body",
                new
                {
                    template.ET_Code,
                    template.Subject,
                    template.Body
                });
            _logger.LogInformation("Template {Code} created", template.ET_Code);
            return result ?? template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template");
            return StatusCode(500, new { message = $"Failed to create template: {ex.Message}" });
        }
    }

    [HttpPut("templates/{id}")]
    public async Task<ActionResult<EmailTemplateRow>> UpdateTemplate(int id, [FromBody] EmailTemplateRow template)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                "exec dbo.csp_Email_Templates_Update @ET_ID, @ET_Code, @Subject, @Body",
                new
                {
                    ET_ID = id,
                    template.ET_Code,
                    template.Subject,
                    template.Body
                });
            _logger.LogInformation("Template {Id} updated", id);
            template.ET_ID = id;
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {Id}", id);
            return StatusCode(500, new { message = $"Failed to update template: {ex.Message}" });
        }
    }

    #endregion

    #region Tasks

    [HttpGet("tasks")]
    public async Task<ActionResult<List<TaskRow>>> GetTasks([FromQuery] int? appId = null)
    {
        try
        {
            using var conn = CreateConnection();
            var tasks = await conn.QueryAsync<TaskRow>(
                "exec dbo.csp_Tasks_Get @App_ID",
                new { App_ID = appId });
            return tasks.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tasks");
            return StatusCode(500, new { message = "Failed to get tasks" });
        }
    }

    [HttpPost("tasks")]
    public async Task<ActionResult<TaskRow>> CreateTask([FromBody] TaskRow task)
    {
        try
        {
            using var conn = CreateConnection();
            var result = await conn.QuerySingleOrDefaultAsync<TaskRow>(
                "exec dbo.csp_Tasks_AddNew @TaskCode, @TaskType, @App_ID, @ProfileID, @TemplateID, @Status, @TestMailTo, @LangCode, @MailFromName, @MailFrom, @MailTo, @MailCC, @MailBCC, @AttachmentProcName",
                new
                {
                    task.TaskCode,
                    task.TaskType,
                    task.App_ID,
                    task.ProfileID,
                    task.TemplateID,
                    task.Status,
                    task.TestMailTo,
                    task.LangCode,
                    task.MailFromName,
                    task.MailFrom,
                    task.MailTo,
                    task.MailCC,
                    task.MailBCC,
                    task.AttachmentProcName
                });
            _logger.LogInformation("Task {Code} created", task.TaskCode);
            return result ?? task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task");
            return StatusCode(500, new { message = "Failed to create task" });
        }
    }

    [HttpPut("tasks/{id}")]
    public async Task<ActionResult<TaskRow>> UpdateTask(int id, [FromBody] TaskRow task)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                "exec dbo.csp_Tasks_Update @Task_ID, @TaskCode, @TaskType, @App_ID, @ProfileID, @TemplateID, @Status, @TestMailTo, @LangCode, @MailFromName, @MailFrom, @MailTo, @MailCC, @MailBCC, @AttachmentProcName",
                new
                {
                    Task_ID = id,
                    task.TaskCode,
                    task.TaskType,
                    task.App_ID,
                    task.ProfileID,
                    task.TemplateID,
                    task.Status,
                    task.TestMailTo,
                    task.LangCode,
                    task.MailFromName,
                    task.MailFrom,
                    task.MailTo,
                    task.MailCC,
                    task.MailBCC,
                    task.AttachmentProcName
                });
            _logger.LogInformation("Task {Id} updated", id);
            task.Task_ID = id;
            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task {Id}", id);
            return StatusCode(500, new { message = "Failed to update task" });
        }
    }

    #endregion

    #region Outbox

    [HttpGet("outbox")]
    public async Task<ActionResult<OutboxListResponse>> GetOutbox(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            using var conn = CreateConnection();
            var items = await conn.QueryAsync<OutboxRow>("exec dbo.csp_Outbox_Get");
            var list = items.ToList();

            var pagedItems = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new OutboxListResponse
            {
                Items = pagedItems,
                TotalCount = list.Count,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(list.Count / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get outbox");
            return StatusCode(500, new { message = "Failed to get outbox" });
        }
    }

    [HttpPut("outbox/{id}")]
    public async Task<ActionResult> UpdateOutbox(int id, [FromBody] OutboxRow item)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                "exec dbo.csp_Outbox_Update @ID, @TaskID, @ToList, @BodyJson, @DetailJson",
                new { ID = id, item.TaskId, item.ToList, item.BodyJson, item.DetailJson });
            _logger.LogInformation("Outbox item {Id} updated", id);
            return Ok(new { message = "Updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update outbox item {Id}", id);
            return StatusCode(500, new { message = "Failed to update outbox item" });
        }
    }

    [HttpDelete("outbox/{id}")]
    public async Task<ActionResult> DeleteOutbox(int id)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync("exec dbo.csp_Outbox_Delete @ID", new { ID = id });
            _logger.LogInformation("Outbox item {Id} deleted", id);
            return Ok(new { message = "Deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete outbox item {Id}", id);
            return StatusCode(500, new { message = "Failed to delete outbox item" });
        }
    }

    #endregion

    #region History

    [HttpGet("history")]
    public async Task<ActionResult<HistoryListResponse>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            using var conn = CreateConnection();
            var items = await conn.QueryAsync<HistoryRow>("exec dbo.csp_History_Get");
            var list = items.ToList();

            var pagedItems = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new HistoryListResponse
            {
                Items = pagedItems,
                TotalCount = list.Count,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(list.Count / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get history");
            return StatusCode(500, new { message = "Failed to get history" });
        }
    }

    [HttpPost("history/{id}/retry")]
    public async Task<ActionResult> RetryHistory(int id)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync("exec dbo.csp_History_Retry @Id", new { Id = id });
            _logger.LogInformation("History item {Id} queued for retry", id);
            return Ok(new { message = "Queued for retry" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry history item {Id}", id);
            return StatusCode(500, new { message = "Failed to retry" });
        }
    }

    [HttpGet("history/{id}/audit")]
    public async Task<ActionResult<List<AuditRow>>> GetHistoryAudit(int id)
    {
        try
        {
            using var conn = CreateConnection();
            var audit = await conn.QueryAsync<AuditRow>(
                "exec dbo.csp_History_Audit @ID",
                new { ID = id });
            return audit.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit for history item {Id}", id);
            return StatusCode(500, new { message = "Failed to get audit" });
        }
    }

    #endregion

    #region Lookup Data

    [HttpGet("lookup/security-modes")]
    public async Task<ActionResult<List<LookupItem>>> GetSecurityModes()
    {
        try
        {
            using var conn = CreateConnection();
            var items = await conn.QueryAsync<LookupItem>("exec dbo.csp_SecurityMode_Get");
            return items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security modes");
            return StatusCode(500, new { message = "Failed to get security modes" });
        }
    }

    [HttpGet("lookup/task-status")]
    public async Task<ActionResult<List<LookupItem>>> GetTaskStatuses()
    {
        try
        {
            using var conn = CreateConnection();
            var items = await conn.QueryAsync<LookupItem>("exec dbo.csp_Task_Status");
            return items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task statuses");
            return StatusCode(500, new { message = "Failed to get task statuses" });
        }
    }

    [HttpGet("lookup/task-types")]
    public async Task<ActionResult<List<LookupItem>>> GetTaskTypes()
    {
        try
        {
            using var conn = CreateConnection();
            var items = await conn.QueryAsync<LookupItem>("exec dbo.csp_Task_Type");
            return items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task types");
            return StatusCode(500, new { message = "Failed to get task types" });
        }
    }

    [HttpGet("lookup/task-priorities")]
    public async Task<ActionResult<List<LookupItem>>> GetTaskPriorities()
    {
        try
        {
            using var conn = CreateConnection();
            var items = await conn.QueryAsync<LookupItem>("exec dbo.csp_Task_Priority");
            return items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task priorities");
            return StatusCode(500, new { message = "Failed to get task priorities" });
        }
    }

    [HttpGet("lookup/outbox-status")]
    public async Task<ActionResult<List<LookupItem>>> GetOutboxStatuses()
    {
        try
        {
            using var conn = CreateConnection();
            var items = await conn.QueryAsync<LookupItem>("exec dbo.csp_Outbox_Status");
            return items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get outbox statuses");
            return StatusCode(500, new { message = "Failed to get outbox statuses" });
        }
    }

    #endregion

    #region Stats

    [HttpGet("stats")]
    public async Task<ActionResult<NotificationStats>> GetStats()
    {
        try
        {
            using var conn = CreateConnection();

            // Get counts from each table
            var profileCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MailProfiles");
            var activeProfileCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MailProfiles WHERE IsActive = 1");
            var templateCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EmailTemplates");
            var taskCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Tasks");
            var activeTaskCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Tasks WHERE Status = 'Active'");
            var pendingCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EmailOutbox WHERE Status = 'Pending'");
            var failedCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EmailOutbox WHERE Status = 'Failed'");

            var today = DateTime.UtcNow.Date;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);

            var sentToday = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM EmailHistory WHERE SentAt >= @Today",
                new { Today = today });
            var sentThisWeek = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM EmailHistory WHERE SentAt >= @ThisWeek",
                new { ThisWeek = thisWeek });

            return new NotificationStats
            {
                TotalProfiles = profileCount,
                ActiveProfiles = activeProfileCount,
                TotalTemplates = templateCount,
                TotalTasks = taskCount,
                ActiveTasks = activeTaskCount,
                PendingMessages = pendingCount,
                FailedMessages = failedCount,
                SentToday = sentToday,
                SentThisWeek = sentThisWeek
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats");
            return StatusCode(500, new { message = "Failed to get stats. Check database connection." });
        }
    }

    #endregion
}

#region DTOs

public class LookupItem
{
    public string? Value { get; set; }
    public string? Text { get; set; }
}

public class OutboxListResponse
{
    public List<OutboxRow> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class HistoryListResponse
{
    public List<HistoryRow> Items { get; set; } = new();
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
