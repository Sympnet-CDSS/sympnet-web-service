using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SympNet.WebApi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly Dictionary<string, string> _userConnections = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections[userId] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections.Remove(userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string receiverId, string message, string messageType = "text")
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(senderId))
            return;

        if (_userConnections.TryGetValue(receiverId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message, messageType, DateTime.UtcNow);
        }
        
        await Clients.Caller.SendAsync("MessageSent", senderId, message, messageType, DateTime.UtcNow);
    }

    public async Task JoinChat(string doctorId, string patientId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{doctorId}_{patientId}");
    }

    public async Task LeaveChat(string doctorId, string patientId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{doctorId}_{patientId}");
    }
}