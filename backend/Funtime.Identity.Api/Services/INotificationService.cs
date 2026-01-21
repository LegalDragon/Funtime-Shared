namespace Funtime.Identity.Api.Services;

/// <summary>
/// Result of a notification delivery attempt
/// </summary>
public class NotificationResult
{
    /// <summary>
    /// Whether the notification was processed without errors
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
    /// Error message if delivery failed
    /// </summary>
    public string? Error { get; set; }

    public static NotificationResult Succeeded(bool userOnline) => new()
    {
        Success = true,
        UserOnline = userOnline,
        Delivered = userOnline // Delivered if user was online
    };

    public static NotificationResult Failed(string error) => new()
    {
        Success = false,
        UserOnline = false,
        Delivered = false,
        Error = error
    };
}

/// <summary>
/// Result of a site-wide notification delivery
/// </summary>
public class SiteNotificationResult
{
    public bool Success { get; set; }
    public bool Delivered { get; set; }
    public int ConnectedUsers { get; set; }
    public string? Error { get; set; }

    public static SiteNotificationResult Succeeded(int connectedUsers) => new()
    {
        Success = true,
        Delivered = true,
        ConnectedUsers = connectedUsers
    };

    public static SiteNotificationResult Failed(string error) => new()
    {
        Success = false,
        Delivered = false,
        ConnectedUsers = 0,
        Error = error
    };
}

/// <summary>
/// Result of a broadcast notification
/// </summary>
public class BroadcastNotificationResult
{
    public bool Success { get; set; }
    public bool Delivered { get; set; }
    public string? Error { get; set; }

    public static BroadcastNotificationResult Succeeded() => new()
    {
        Success = true,
        Delivered = true
    };

    public static BroadcastNotificationResult Failed(string error) => new()
    {
        Success = false,
        Delivered = false,
        Error = error
    };
}

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    /// <returns>Result containing delivery status</returns>
    Task<NotificationResult> SendToUserAsync(int userId, string type, object payload);

    /// <summary>
    /// Send a notification to all users on a specific site
    /// </summary>
    /// <returns>Result containing delivery status and connected user count</returns>
    Task<SiteNotificationResult> SendToSiteAsync(string siteKey, string type, object payload);

    /// <summary>
    /// Send a notification to all connected users
    /// </summary>
    /// <returns>Result containing delivery status</returns>
    Task<BroadcastNotificationResult> SendToAllAsync(string type, object payload);

    /// <summary>
    /// Check if a user is currently connected
    /// </summary>
    bool IsUserConnected(int userId);

    /// <summary>
    /// Get the count of users connected to a specific site
    /// </summary>
    int GetSiteConnectionCount(string siteKey);
}
