using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Funtime.Identity.Api.Services;
using System.Security.Claims;

namespace Funtime.Identity.Api.Controllers;

/// <summary>
/// API endpoints for sending real-time push notifications via SignalR.
/// External sites can call these endpoints to send notifications to users.
/// </summary>
[ApiController]
[Route("api/push")]
public class PushNotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PushNotificationController> _logger;
    private readonly IConfiguration _configuration;

    public PushNotificationController(
        INotificationService notificationService,
        ILogger<PushNotificationController> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Send a notification to a specific user by their user ID
    /// </summary>
    [HttpPost("user/{userId}")]
    [Authorize]
    public async Task<ActionResult> SendToUser(int userId, [FromBody] PushNotificationRequest request)
    {
        if (!IsAuthorizedToSend())
        {
            return Forbid();
        }

        await _notificationService.SendToUserAsync(userId, request.Type, request.Payload);

        _logger.LogInformation(
            "Push notification sent to user {UserId}, type: {Type}",
            userId, request.Type);

        return Ok(new {
            success = true,
            message = "Notification sent",
            isUserConnected = _notificationService.IsUserConnected(userId)
        });
    }

    /// <summary>
    /// Send a notification to all users on a specific site
    /// </summary>
    [HttpPost("site/{siteKey}")]
    [Authorize]
    public async Task<ActionResult> SendToSite(string siteKey, [FromBody] PushNotificationRequest request)
    {
        if (!IsAuthorizedToSend(siteKey))
        {
            return Forbid();
        }

        await _notificationService.SendToSiteAsync(siteKey, request.Type, request.Payload);

        _logger.LogInformation(
            "Push notification sent to site {SiteKey}, type: {Type}",
            siteKey, request.Type);

        return Ok(new { success = true, message = "Notification sent to site" });
    }

    /// <summary>
    /// Send a notification to all connected users (admin only)
    /// </summary>
    [HttpPost("broadcast")]
    [Authorize(Roles = "SU")]
    public async Task<ActionResult> Broadcast([FromBody] PushNotificationRequest request)
    {
        await _notificationService.SendToAllAsync(request.Type, request.Payload);

        _logger.LogInformation(
            "Broadcast notification sent, type: {Type}",
            request.Type);

        return Ok(new { success = true, message = "Broadcast sent" });
    }

    /// <summary>
    /// Check if a user is currently connected to receive notifications
    /// </summary>
    [HttpGet("user/{userId}/status")]
    [Authorize]
    public ActionResult<UserConnectionStatus> GetUserStatus(int userId)
    {
        if (!IsAuthorizedToSend())
        {
            return Forbid();
        }

        return Ok(new UserConnectionStatus
        {
            UserId = userId,
            IsConnected = _notificationService.IsUserConnected(userId)
        });
    }

    /// <summary>
    /// Send notifications to multiple users at once
    /// </summary>
    [HttpPost("users/batch")]
    [Authorize]
    public async Task<ActionResult> SendToUsers([FromBody] BatchNotificationRequest request)
    {
        if (!IsAuthorizedToSend())
        {
            return Forbid();
        }

        var results = new List<BatchNotificationResult>();

        foreach (var userId in request.UserIds)
        {
            await _notificationService.SendToUserAsync(userId, request.Type, request.Payload);
            results.Add(new BatchNotificationResult
            {
                UserId = userId,
                IsConnected = _notificationService.IsUserConnected(userId)
            });
        }

        _logger.LogInformation(
            "Batch notification sent to {Count} users, type: {Type}",
            request.UserIds.Count, request.Type);

        return Ok(new {
            success = true,
            message = $"Notification sent to {request.UserIds.Count} users",
            results
        });
    }

    /// <summary>
    /// Check if the current user is authorized to send notifications.
    /// Allows: SU role, admin role, or users with valid API key
    /// </summary>
    private bool IsAuthorizedToSend(string? siteKey = null)
    {
        // Super users can always send
        if (User.IsInRole("SU"))
        {
            return true;
        }

        // Check for API key header (for server-to-server calls)
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
        {
            var validApiKey = _configuration["PushNotifications:ApiKey"];
            if (!string.IsNullOrEmpty(validApiKey) && apiKey == validApiKey)
            {
                return true;
            }
        }

        // Site admins can send to their own site
        if (!string.IsNullOrEmpty(siteKey))
        {
            var userSites = User.FindFirst("sites")?.Value;
            if (!string.IsNullOrEmpty(userSites))
            {
                try
                {
                    var sites = System.Text.Json.JsonSerializer.Deserialize<List<string>>(userSites);
                    if (sites?.Contains(siteKey) == true)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }
        }

        // For now, allow any authenticated user to send notifications
        // You can make this more restrictive based on your requirements
        return User.Identity?.IsAuthenticated == true;
    }
}

#region DTOs

public class PushNotificationRequest
{
    /// <summary>
    /// Type of notification (e.g., "message", "alert", "update")
    /// </summary>
    public string Type { get; set; } = "notification";

    /// <summary>
    /// Notification payload - can be any JSON object
    /// </summary>
    public object? Payload { get; set; }
}

public class BatchNotificationRequest
{
    /// <summary>
    /// List of user IDs to send to
    /// </summary>
    public List<int> UserIds { get; set; } = new();

    /// <summary>
    /// Type of notification
    /// </summary>
    public string Type { get; set; } = "notification";

    /// <summary>
    /// Notification payload
    /// </summary>
    public object? Payload { get; set; }
}

public class UserConnectionStatus
{
    public int UserId { get; set; }
    public bool IsConnected { get; set; }
}

public class BatchNotificationResult
{
    public int UserId { get; set; }
    public bool IsConnected { get; set; }
}

#endregion
