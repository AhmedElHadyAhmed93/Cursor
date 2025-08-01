using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Infrastructure.Services.SignalR;
using System.Security.Claims;

namespace Api.Hubs;

[Authorize]
public class SocketHub : Hub
{
    private readonly IConnectionTrackingService _connectionTracking;
    private readonly ILogger<SocketHub> _logger;

    public SocketHub(IConnectionTrackingService connectionTracking, ILogger<SocketHub> logger)
    {
        _connectionTracking = connectionTracking;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await _connectionTracking.AddConnectionAsync(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            
            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await _connectionTracking.RemoveConnectionAsync(userId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            
            _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId, Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Join(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserJoined", Context.User?.Identity?.Name, groupName);
        
        _logger.LogInformation("User {User} joined group {GroupName}", Context.User?.Identity?.Name, groupName);
    }

    public async Task Leave(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserLeft", Context.User?.Identity?.Name, groupName);
        
        _logger.LogInformation("User {User} left group {GroupName}", Context.User?.Identity?.Name, groupName);
    }

    public async Task SendToAll(string message)
    {
        var user = Context.User?.Identity?.Name ?? "Anonymous";
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        await Clients.All.SendAsync("ReceiveMessage", user, message, DateTime.UtcNow);
        
        _logger.LogInformation("User {User} sent message to all: {Message}", user, message);
    }

    public async Task SendToUser(string targetUserId, string message)
    {
        var senderUser = Context.User?.Identity?.Name ?? "Anonymous";
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var connections = await _connectionTracking.GetConnectionsAsync(targetUserId);
        if (connections.Any())
        {
            await Clients.Clients(connections).SendAsync("ReceiveMessage", senderUser, message, DateTime.UtcNow);
            _logger.LogInformation("User {Sender} sent message to user {Target}: {Message}", senderUser, targetUserId, message);
        }
        else
        {
            await Clients.Caller.SendAsync("UserOffline", targetUserId);
            _logger.LogWarning("User {Target} is offline, message from {Sender} not delivered", targetUserId, senderUser);
        }
    }

    public async Task SendToGroup(string groupName, string message)
    {
        var user = Context.User?.Identity?.Name ?? "Anonymous";
        
        await Clients.Group(groupName).SendAsync("ReceiveMessage", user, message, DateTime.UtcNow);
        
        _logger.LogInformation("User {User} sent message to group {GroupName}: {Message}", user, groupName, message);
    }
}