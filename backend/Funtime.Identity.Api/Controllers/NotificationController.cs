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

    private static string GenerateApiKey()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return "fxn_" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [HttpGet("apps")]
    public async Task<ActionResult<List<AppResponseDto>>> GetApps()
    {
        try
        {
            using var conn = CreateConnection();
            var apps = await conn.QueryAsync<AppRow>(
                @"SELECT App_ID, App_Code, Descr, ProfileID, ApiKey, AllowedTasks, IsActive, CreatedAt, LastUsedAt, RequestCount, Notes
                  FROM dbo.Apps ORDER BY App_ID");
            return apps.Select(a => AppResponseDto.FromRow(a)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get apps");
            return StatusCode(500, new { message = "Failed to get applications" });
        }
    }

    [HttpPost("apps")]
    public async Task<ActionResult<AppResponseDto>> CreateApp([FromBody] AppRow app)
    {
        try
        {
            using var conn = CreateConnection();
            var newKey = GenerateApiKey();
            var id = await conn.QuerySingleAsync<int>(
                @"INSERT INTO dbo.Apps (App_Code, Descr, ProfileID, ApiKey, IsActive, CreatedAt, RequestCount, Notes)
                  OUTPUT INSERTED.App_ID
                  VALUES (@App_Code, @Descr, @ProfileID, @ApiKey, 1, GETUTCDATE(), 0, @Notes)",
                new { app.App_Code, app.Descr, app.ProfileID, ApiKey = newKey, app.Notes });
            _logger.LogInformation("App {AppCode} created with ID {Id}", app.App_Code, id);

            var created = await conn.QueryFirstAsync<AppRow>(
                @"SELECT App_ID, App_Code, Descr, ProfileID, ApiKey, AllowedTasks, IsActive, CreatedAt, LastUsedAt, RequestCount, Notes
                  FROM dbo.Apps WHERE App_ID = @Id", new { Id = id });
            return AppResponseDto.FromRow(created, newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create app");
            return StatusCode(500, new { message = "Failed to create application" });
        }
    }

    [HttpPut("apps/{id}")]
    public async Task<ActionResult<AppResponseDto>> UpdateApp(int id, [FromBody] AppRow app)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                @"UPDATE dbo.Apps SET App_Code = @App_Code, Descr = @Descr, ProfileID = @ProfileID, Notes = @Notes
                  WHERE App_ID = @App_ID",
                new { App_ID = id, app.App_Code, app.Descr, app.ProfileID, app.Notes });
            _logger.LogInformation("App {Id} updated", id);

            var updated = await conn.QueryFirstAsync<AppRow>(
                @"SELECT App_ID, App_Code, Descr, ProfileID, ApiKey, AllowedTasks, IsActive, CreatedAt, LastUsedAt, RequestCount, Notes
                  FROM dbo.Apps WHERE App_ID = @Id", new { Id = id });
            return AppResponseDto.FromRow(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update app {Id}", id);
            return StatusCode(500, new { message = "Failed to update application" });
        }
    }

    [HttpPost("apps/{id}/toggle")]
    public async Task<ActionResult<AppResponseDto>> ToggleApp(int id)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE dbo.Apps SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE App_ID = @Id",
                new { Id = id });

            var updated = await conn.QueryFirstOrDefaultAsync<AppRow>(
                @"SELECT App_ID, App_Code, Descr, ProfileID, ApiKey, AllowedTasks, IsActive, CreatedAt, LastUsedAt, RequestCount, Notes
                  FROM dbo.Apps WHERE App_ID = @Id", new { Id = id });
            if (updated == null) return NotFound(new { message = $"App {id} not found" });

            _logger.LogInformation("App {Id} toggled to {Status}", id, updated.IsActive ? "active" : "inactive");
            return AppResponseDto.FromRow(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle app {Id}", id);
            return StatusCode(500, new { message = "Failed to toggle application" });
        }
    }

    [HttpPost("apps/{id}/regenerate-key")]
    public async Task<ActionResult<AppResponseDto>> RegenerateAppKey(int id)
    {
        try
        {
            using var conn = CreateConnection();
            var newKey = GenerateApiKey();
            var affected = await conn.ExecuteAsync(
                "UPDATE dbo.Apps SET ApiKey = @ApiKey WHERE App_ID = @Id",
                new { ApiKey = newKey, Id = id });
            if (affected == 0) return NotFound(new { message = $"App {id} not found" });

            var updated = await conn.QueryFirstAsync<AppRow>(
                @"SELECT App_ID, App_Code, Descr, ProfileID, ApiKey, AllowedTasks, IsActive, CreatedAt, LastUsedAt, RequestCount, Notes
                  FROM dbo.Apps WHERE App_ID = @Id", new { Id = id });

            _logger.LogInformation("App {Id} API key regenerated", id);
            return AppResponseDto.FromRow(updated, newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate key for app {Id}", id);
            return StatusCode(500, new { message = "Failed to regenerate API key" });
        }
    }

    [HttpGet("apps/{id}/profiles")]
    public async Task<ActionResult<List<int>>> GetAppProfiles(int id)
    {
        try
        {
            using var conn = CreateConnection();
            var profileIds = (await conn.QueryAsync<int>(
                "SELECT ProfileId FROM dbo.AppProfiles WHERE AppId = @AppId",
                new { AppId = id })).ToList();
            return profileIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profiles for app {Id}", id);
            return StatusCode(500, new { message = "Failed to get app profiles" });
        }
    }

    [HttpPut("apps/{id}/profiles")]
    public async Task<ActionResult<List<int>>> UpdateAppProfiles(int id, [FromBody] List<int> profileIds)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            await conn.ExecuteAsync(
                "DELETE FROM dbo.AppProfiles WHERE AppId = @AppId",
                new { AppId = id }, tx);

            foreach (var pid in profileIds)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO dbo.AppProfiles (AppId, ProfileId) VALUES (@AppId, @ProfileId)",
                    new { AppId = id, ProfileId = pid }, tx);
            }

            tx.Commit();
            _logger.LogInformation("App {Id} profiles updated: [{Profiles}]", id, string.Join(", ", profileIds));
            return profileIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profiles for app {Id}", id);
            return StatusCode(500, new { message = "Failed to update app profiles" });
        }
    }

    [HttpDelete("apps/{id}/key")]
    public async Task<ActionResult> RevokeAppKey(int id)
    {
        try
        {
            using var conn = CreateConnection();
            var affected = await conn.ExecuteAsync(
                "UPDATE dbo.Apps SET ApiKey = NULL WHERE App_ID = @Id",
                new { Id = id });
            if (affected == 0) return NotFound(new { message = $"App {id} not found" });

            _logger.LogInformation("App {Id} API key revoked", id);
            return Ok(new { message = "API key revoked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke key for app {Id}", id);
            return StatusCode(500, new { message = "Failed to revoke API key" });
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
            var parameters = new DynamicParameters();
            parameters.Add("@ET_Code", template.ET_Code ?? "");
            parameters.Add("@App_ID", template.App_ID ?? 0);
            parameters.Add("@Lang_Code", template.Lang_Code ?? "en");
            parameters.Add("@SendFrom", template.SendFrom ?? "");
            parameters.Add("@SendTo", template.SendTo ?? "");
            parameters.Add("@BCC", template.BCC ?? "");
            parameters.Add("@Subject", template.Subject ?? "");
            parameters.Add("@Body", template.Body ?? "");

            var result = await conn.QuerySingleOrDefaultAsync<EmailTemplateRow>(
                "exec dbo.csp_Email_Templates_AddNew @ET_Code, @App_ID, @Lang_Code, @SendFrom, @SendTo, @BCC, @Subject, @Body",
                parameters);
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
            var parameters = new DynamicParameters();
            parameters.Add("@ET_ID", id);
            parameters.Add("@ET_Code", template.ET_Code ?? "");
            parameters.Add("@App_ID", template.App_ID ?? 0);
            parameters.Add("@Lang_Code", template.Lang_Code ?? "en");
            parameters.Add("@SendFrom", template.SendFrom ?? "");
            parameters.Add("@SendTo", template.SendTo ?? "");
            parameters.Add("@BCC", template.BCC ?? "");
            parameters.Add("@Subject", template.Subject ?? "");
            parameters.Add("@Body", template.Body ?? "");

            await conn.ExecuteAsync(
                "exec dbo.csp_Email_Templates_Update @ET_ID, @ET_Code, @App_ID, @Lang_Code, @SendFrom, @SendTo, @BCC, @Subject, @Body",
                parameters);
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
                new { App_ID = appId ?? 0 });
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
            var parameters = new DynamicParameters();
            parameters.Add("@TaskCode", task.TaskCode ?? "");
            parameters.Add("@TaskType", task.TaskType ?? "E");
            parameters.Add("@App_ID", task.App_ID ?? 0);
            parameters.Add("@ProfileID", task.ProfileID ?? 0);
            parameters.Add("@TemplateID", task.TemplateID ?? 0);
            parameters.Add("@Status", task.Status ?? "A");
            parameters.Add("@MailPriority", task.MailPriority ?? "N");
            parameters.Add("@TestMailTo", task.TestMailTo ?? "");
            parameters.Add("@LangCode", task.LangCode ?? "en");
            parameters.Add("@MailFromName", task.MailFromName ?? "");
            parameters.Add("@MailFrom", task.MailFrom ?? "");
            parameters.Add("@MailTo", task.MailTo ?? "");
            parameters.Add("@MailCC", task.MailCC ?? "");
            parameters.Add("@MailBCC", task.MailBCC ?? "");
            parameters.Add("@AttachmentProcName", task.AttachmentProcName ?? "");

            var result = await conn.QuerySingleOrDefaultAsync<TaskRow>(
                "exec dbo.csp_Tasks_AddNew @TaskCode, @TaskType, @App_ID, @ProfileID, @TemplateID, @Status, @MailPriority, @TestMailTo, @LangCode, @MailFromName, @MailFrom, @MailTo, @MailCC, @MailBCC, @AttachmentProcName",
                parameters);
            _logger.LogInformation("Task {Code} created", task.TaskCode);
            return result ?? task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task");
            return StatusCode(500, new { message = $"Failed to create task: {ex.Message}" });
        }
    }

    [HttpPut("tasks/{id}")]
    public async Task<ActionResult<TaskRow>> UpdateTask(int id, [FromBody] TaskRow task)
    {
        try
        {
            using var conn = CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Task_ID", id);
            parameters.Add("@TaskCode", task.TaskCode ?? "");
            parameters.Add("@TaskType", task.TaskType ?? "E");
            parameters.Add("@App_ID", task.App_ID ?? 0);
            parameters.Add("@ProfileID", task.ProfileID ?? 0);
            parameters.Add("@TemplateID", task.TemplateID ?? 0);
            parameters.Add("@Status", task.Status ?? "A");
            parameters.Add("@MailPriority", task.MailPriority ?? "N");
            parameters.Add("@TestMailTo", task.TestMailTo ?? "");
            parameters.Add("@LangCode", task.LangCode ?? "en");
            parameters.Add("@MailFromName", task.MailFromName ?? "");
            parameters.Add("@MailFrom", task.MailFrom ?? "");
            parameters.Add("@MailTo", task.MailTo ?? "");
            parameters.Add("@MailCC", task.MailCC ?? "");
            parameters.Add("@MailBCC", task.MailBCC ?? "");
            parameters.Add("@AttachmentProcName", task.AttachmentProcName ?? "");

            await conn.ExecuteAsync("exec dbo.csp_Tasks_Update @Task_ID, @TaskCode, @TaskType, @App_ID, @ProfileID, @TemplateID, @Status, @MailPriority, @TestMailTo, @LangCode, @MailFromName, @MailFrom, @MailTo, @MailCC, @MailBCC, @AttachmentProcName", parameters);
            _logger.LogInformation("Task {Id} updated", id);
            task.Task_ID = id;
            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task {Id}", id);
            return StatusCode(500, new { message = $"Failed to update task: {ex.Message}" });
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
            var list = items.OrderByDescending(x => x.Id).ToList();

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

    [HttpPost("outbox/{id}/retry")]
    public async Task<ActionResult> RetryOutbox(int id)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync("exec dbo.csp_History_Retry @Id", new { Id = id });
            _logger.LogInformation("Outbox item {Id} queued for retry", id);
            return Ok(new { message = "Queued for retry" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry outbox item {Id}", id);
            return StatusCode(500, new { message = "Failed to retry" });
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
            var list = items.OrderByDescending(x => x.ID).ToList();

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
            var profileCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MailProfile");
            var activeProfileCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MailProfile WHERE IsActive = 1");
            var templateCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Email_Templates");
            var taskCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EmailTaskConfig");
            var activeTaskCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EmailTaskConfig WHERE Status = 'Active'");
            var pendingCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EmailOutbox WHERE Status = 1");
            var failedCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EmailOutbox WHERE Status = 99");

            var today = DateTime.UtcNow.Date;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);

            var sentToday = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM EmailOutbox WHERE Status = 100 AND CreatedAt >= @Today",
                new { Today = today });
            var sentThisWeek = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM EmailOutbox WHERE Status = 100 AND CreatedAt >= @ThisWeek",
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
