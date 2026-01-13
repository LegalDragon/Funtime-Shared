using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Funtime.Identity.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private static readonly Dictionary<int, HashSet<string>> _userConnections = new();
    private static readonly object _lock = new();

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            lock (_lock)
            {
                if (!_userConnections.ContainsKey(userId.Value))
                {
                    _userConnections[userId.Value] = new HashSet<string>();
                }
                _userConnections[userId.Value].Add(Context.ConnectionId);
            }

            // Add user to their personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}",
                userId.Value, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            lock (_lock)
            {
                if (_userConnections.ContainsKey(userId.Value))
                {
                    _userConnections[userId.Value].Remove(Context.ConnectionId);
                    if (_userConnections[userId.Value].Count == 0)
                    {
                        _userConnections.Remove(userId.Value);
                    }
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");

            _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}",
                userId.Value, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a site-specific notification group
    /// </summary>
    public async Task JoinSiteGroup(string siteKey)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"site_{siteKey}");
        _logger.LogDebug("Connection {ConnectionId} joined site group {SiteKey}",
            Context.ConnectionId, siteKey);
    }

    /// <summary>
    /// Leave a site-specific notification group
    /// </summary>
    public async Task LeaveSiteGroup(string siteKey)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"site_{siteKey}");
        _logger.LogDebug("Connection {ConnectionId} left site group {SiteKey}",
            Context.ConnectionId, siteKey);
    }

    /// <summary>
    /// Get list of connection IDs for a user
    /// </summary>
    public static IEnumerable<string> GetConnectionsForUser(int userId)
    {
        lock (_lock)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                return connections.ToList();
            }
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Check if a user is currently connected
    /// </summary>
    public static bool IsUserConnected(int userId)
    {
        lock (_lock)
        {
            return _userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0;
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
