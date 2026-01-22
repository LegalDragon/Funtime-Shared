using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Funtime.Identity.Api.Auth;
using Funtime.Identity.Api.Models;
using Funtime.Identity.Api.Services;
using System.Security.Claims;

namespace Funtime.Identity.Api.Controllers;

/// <summary>
/// API endpoints for sending real-time push notifications via SignalR.
/// External sites can call these endpoints to send notifications to users.
/// Supports API key authentication with push:send scope.
/// </summary>
[ApiController]
[Route("api/push")]
public class PushNotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PushNotificationController> _logger;

    public PushNotificationController(
        INotificationService notificationService,
        ILogger<PushNotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Send a notification to a specific user by their user ID (supports API key with push:send scope)
    /// </summary>
    [HttpPost("user/{userId}")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public async Task<ActionResult<PushResponse>> SendToUser(int userId, [FromBody] PushNotificationRequest request)
    {
        var result = await _notificationService.SendToUserAsync(userId, request.Type, request.Payload);

        _logger.LogInformation(
            "Push notification sent to user {UserId}, type: {Type}, online: {Online}, delivered: {Delivered}",
            userId, request.Type, result.UserOnline, result.Delivered);

        return Ok(new PushResponse
        {
            Success = result.Success,
            UserOnline = result.UserOnline,
            Delivered = result.Delivered,
            Error = result.Error
        });
    }

    /// <summary>
    /// Send a notification to all users on a specific site (supports API key with push:send scope)
    /// </summary>
    [HttpPost("site/{siteKey}")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public async Task<ActionResult<SitePushResponse>> SendToSite(string siteKey, [FromBody] PushNotificationRequest request)
    {
        var result = await _notificationService.SendToSiteAsync(siteKey, request.Type, request.Payload);

        _logger.LogInformation(
            "Push notification sent to site {SiteKey}, type: {Type}, connected: {Connected}, delivered: {Delivered}",
            siteKey, request.Type, result.ConnectedUsers, result.Delivered);

        return Ok(new SitePushResponse
        {
            Success = result.Success,
            Delivered = result.Delivered,
            ConnectedUsers = result.ConnectedUsers,
            Error = result.Error
        });
    }

    /// <summary>
    /// Send a notification to all connected users (supports API key with push:send scope)
    /// </summary>
    [HttpPost("broadcast")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public async Task<ActionResult<BroadcastPushResponse>> Broadcast([FromBody] PushNotificationRequest request)
    {
        var result = await _notificationService.SendToAllAsync(request.Type, request.Payload);

        _logger.LogInformation(
            "Broadcast notification sent, type: {Type}, delivered: {Delivered}",
            request.Type, result.Delivered);

        return Ok(new BroadcastPushResponse
        {
            Success = result.Success,
            Delivered = result.Delivered,
            Error = result.Error
        });
    }

    /// <summary>
    /// Check if a user is currently connected to receive notifications (supports API key with push:send scope)
    /// </summary>
    [HttpGet("user/{userId}/status")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public ActionResult<UserConnectionStatus> GetUserStatus(int userId)
    {
        return Ok(new UserConnectionStatus
        {
            UserId = userId,
            IsConnected = _notificationService.IsUserConnected(userId)
        });
    }

    /// <summary>
    /// Send notifications to multiple users at once (supports API key with push:send scope)
    /// </summary>
    [HttpPost("users/batch")]
    [ApiKeyAuthorize(ApiScopes.PushSend, AllowJwt = true)]
    public async Task<ActionResult<BatchPushResponse>> SendToUsers([FromBody] BatchNotificationRequest request)
    {
        var results = new List<BatchNotificationResult>();
        var onlineCount = 0;
        var deliveredCount = 0;

        foreach (var userId in request.UserIds)
        {
            var result = await _notificationService.SendToUserAsync(userId, request.Type, request.Payload);

            if (result.UserOnline) onlineCount++;
            if (result.Delivered) deliveredCount++;

            results.Add(new BatchNotificationResult
            {
                UserId = userId,
                UserOnline = result.UserOnline,
                Delivered = result.Delivered
            });
        }

        _logger.LogInformation(
            "Batch notification sent to {Count} users, type: {Type}, online: {Online}, delivered: {Delivered}",
            request.UserIds.Count, request.Type, onlineCount, deliveredCount);

        return Ok(new BatchPushResponse
        {
            Success = true,
            TotalUsers = request.UserIds.Count,
            OnlineUsers = onlineCount,
            DeliveredCount = deliveredCount,
            Results = results
        });
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

/// <summary>
/// Response from a push notification attempt
/// </summary>
public class PushResponse
{
    /// <summary>
    /// Whether the notification was processed successfully (no errors)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether the user was connected at the time of sending
    /// </summary>
    public bool UserOnline { get; set; }

    /// <summary>
    /// Whether SignalR successfully delivered the notification
    /// </summary>
    public bool Delivered { get; set; }

    /// <summary>
    /// Error message if Success is false
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Response from a site-wide push notification
/// </summary>
public class SitePushResponse
{
    /// <summary>
    /// Whether the notification was processed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether SignalR successfully sent to the site group
    /// </summary>
    public bool Delivered { get; set; }

    /// <summary>
    /// Number of users currently connected to this site
    /// </summary>
    public int ConnectedUsers { get; set; }

    /// <summary>
    /// Error message if Success is false
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Response from a broadcast notification
/// </summary>
public class BroadcastPushResponse
{
    /// <summary>
    /// Whether the notification was processed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether SignalR successfully broadcast the notification
    /// </summary>
    public bool Delivered { get; set; }

    /// <summary>
    /// Error message if Success is false
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Result for each user in a batch notification
/// </summary>
public class BatchNotificationResult
{
    public int UserId { get; set; }
    public bool UserOnline { get; set; }
    public bool Delivered { get; set; }
}

/// <summary>
/// Response from a batch notification request
/// </summary>
public class BatchPushResponse
{
    /// <summary>
    /// Whether the batch was processed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total users in the batch
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Number of users who were online
    /// </summary>
    public int OnlineUsers { get; set; }

    /// <summary>
    /// Number of successful deliveries
    /// </summary>
    public int DeliveredCount { get; set; }

    /// <summary>
    /// Individual results per user
    /// </summary>
    public List<BatchNotificationResult> Results { get; set; } = new();
}

#endregion
