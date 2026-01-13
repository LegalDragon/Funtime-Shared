namespace Funtime.Identity.Api.Services;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    Task SendToUserAsync(int userId, string type, object payload);

    /// <summary>
    /// Send a notification to all users on a specific site
    /// </summary>
    Task SendToSiteAsync(string siteKey, string type, object payload);

    /// <summary>
    /// Send a notification to all connected users
    /// </summary>
    Task SendToAllAsync(string type, object payload);

    /// <summary>
    /// Check if a user is currently connected
    /// </summary>
    bool IsUserConnected(int userId);
}
