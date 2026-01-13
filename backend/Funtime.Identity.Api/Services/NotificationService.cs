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

    public async Task SendToUserAsync(int userId, string type, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    Type = type,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent notification type {Type} to user {UserId}", type, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }

    public async Task SendToSiteAsync(string siteKey, string type, object payload)
    {
        try
        {
            await _hubContext.Clients.Group($"site_{siteKey}")
                .SendAsync("ReceiveNotification", new
                {
                    Type = type,
                    Payload = payload,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent notification type {Type} to site {SiteKey}", type, siteKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to site {SiteKey}", siteKey);
        }
    }

    public async Task SendToAllAsync(string type, object payload)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to all users");
        }
    }

    public bool IsUserConnected(int userId)
    {
        return NotificationHub.IsUserConnected(userId);
    }
}
