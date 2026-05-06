using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SympNet.WebApi.Hubs;

[Authorize]
public class WebRTCHub : Hub
{
    private static readonly Dictionary<string, string> _userConnections = new();

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
            _userConnections[userId] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections.Remove(userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendOffer(string targetUserId, string sdp)
    {
        var fromUserId = GetUserId();
        if (_userConnections.TryGetValue(targetUserId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveOffer", fromUserId, sdp);
        }
    }

    public async Task SendAnswer(string targetUserId, string sdp)
    {
        var fromUserId = GetUserId();
        if (_userConnections.TryGetValue(targetUserId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveAnswer", fromUserId, sdp);
        }
    }

    public async Task SendIceCandidate(string targetUserId, string candidate, string sdpMid, int sdpMLineIndex)
    {
        var fromUserId = GetUserId();
        if (_userConnections.TryGetValue(targetUserId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveIceCandidate", fromUserId, candidate, sdpMid, sdpMLineIndex);
        }
    }
}
