using Microsoft.AspNetCore.SignalR;
using Funtime.Identity.Api.Hubs;

namespace Funtime.Identity.Api.Services;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<NotificationResult> SendToUserAsync(int userId, string type, object payload)
    {
        var userOnline = IsUserConnected(userId);

        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    Type = type,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent notification type {Type} to user {UserId}, online: {Online}",
                type, userId, userOnline);

            return NotificationResult.Succeeded(userOnline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            return NotificationResult.Failed(ex.Message);
        }
    }

    public async Task<SiteNotificationResult> SendToSiteAsync(string siteKey, string type, object payload)
    {
        var connectedUsers = GetSiteConnectionCount(siteKey);

        try
        {
            await _hubContext.Clients.Group($"site_{siteKey}")
                .SendAsync("ReceiveNotification", new
                {
                    Type = type,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent notification type {Type} to site {SiteKey}, connected users: {Count}",
                type, siteKey, connectedUsers);

            return SiteNotificationResult.Succeeded(connectedUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to site {SiteKey}", siteKey);
            return SiteNotificationResult.Failed(ex.Message);
        }
    }

    public async Task<BroadcastNotificationResult> SendToAllAsync(string type, object payload)
    {
        try
        {
            await _hubContext.Clients.All
                .SendAsync("ReceiveNotification", new
                {
                    Type = type,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent notification type {Type} to all users", type);

            return BroadcastNotificationResult.Succeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to all users");
            return BroadcastNotificationResult.Failed(ex.Message);
        }
    }

    public bool IsUserConnected(int userId)
    {
        return NotificationHub.IsUserConnected(userId);
    }

    public int GetSiteConnectionCount(string siteKey)
    {
        return NotificationHub.GetSiteConnectionCount(siteKey);
    }
}
