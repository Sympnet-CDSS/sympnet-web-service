using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SympNet.WebApi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly Dictionary<string, string> _onlineUsers = new();

    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            _onlineUsers[userId] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await Clients.All.SendAsync("UserOnline", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            _onlineUsers.Remove(userId);
            await Clients.All.SendAsync("UserOffline", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string receiverId, string content)
    {
        var senderId = GetUserId();
        if (string.IsNullOrEmpty(senderId)) return;

        if (_onlineUsers.TryGetValue(receiverId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", new
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsMine = false
            });
        }
    }

    public async Task UserTyping(string receiverId)
    {
        var senderId = GetUserId();
        if (string.IsNullOrEmpty(senderId)) return;

        if (_onlineUsers.TryGetValue(receiverId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("UserTyping", new
            {
                UserId = senderId,
                IsTyping = true
            });
        }
    }

    public async Task UserStoppedTyping(string receiverId)
    {
        var senderId = GetUserId();
        if (string.IsNullOrEmpty(senderId)) return;

        if (_onlineUsers.TryGetValue(receiverId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("UserTyping", new
            {
                UserId = senderId,
                IsTyping = false
            });
        }
    }
}
