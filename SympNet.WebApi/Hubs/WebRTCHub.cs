using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SympNet.WebApi.Hubs;

[Authorize]
public class VideoCallHub : Hub
{
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
        await Clients.OthersInGroup($"room_{roomId}")
            .SendAsync("UserJoinedRoom", new
            {
                connectionId = Context.ConnectionId,
                userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                name = Context.User?.FindFirst(ClaimTypes.Name)?.Value,
            });
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
        await Clients.OthersInGroup($"room_{roomId}")
            .SendAsync("UserLeftRoom", Context.ConnectionId);
    }

    // WebRTC signaling
    public async Task SendOffer(string roomId, string targetConnectionId, object sdpOffer)
    {
        await Clients.Client(targetConnectionId)
            .SendAsync("ReceiveOffer", Context.ConnectionId, sdpOffer);
    }

    public async Task SendAnswer(string targetConnectionId, object sdpAnswer)
    {
        await Clients.Client(targetConnectionId)
            .SendAsync("ReceiveAnswer", Context.ConnectionId, sdpAnswer);
    }

    public async Task SendIceCandidate(string targetConnectionId, object candidate)
    {
        await Clients.Client(targetConnectionId)
            .SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
    }

    public async Task ToggleVideo(string roomId, bool isEnabled)
    {
        await Clients.OthersInGroup($"room_{roomId}")
            .SendAsync("ParticipantVideoToggled", Context.ConnectionId, isEnabled);
    }

    public async Task ToggleAudio(string roomId, bool isEnabled)
    {
        await Clients.OthersInGroup($"room_{roomId}")
            .SendAsync("ParticipantAudioToggled", Context.ConnectionId, isEnabled);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
