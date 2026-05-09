using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SympNet.WebApi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public async Task JoinConsultation(string consultationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"consultation_{consultationId}");
        await Clients.Group($"consultation_{consultationId}")
            .SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task LeaveConsultation(string consultationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"consultation_{consultationId}");
    }

    public async Task SendMessage(string consultationId, string message, bool isVoice = false)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        var senderRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        await Clients.Group($"consultation_{consultationId}")
            .SendAsync("ReceiveMessage", new
            {
                senderId,
                senderName,
                senderRole,
                content = message,
                isVoice,
                sentAt = DateTime.UtcNow,
            });
    }

    public async Task SendTyping(string consultationId, bool isTyping)
    {
        var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        await Clients.OthersInGroup($"consultation_{consultationId}")
            .SendAsync("UserTyping", new { senderName, isTyping });
    }

    // --- WebRTC Signaling ---
    public async Task SendOffer(string targetUserId, string sdp)
    {
        var callerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var callerName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        
        await Clients.User(targetUserId).SendAsync("ReceiveOffer", callerId, sdp);
        await Clients.User(targetUserId).SendAsync("IncomingCall", new 
        { 
            SessionId = Guid.NewGuid(), 
            CallerId = callerId, 
            CallerName = callerName 
        });
    }

    public async Task SendAnswer(string targetUserId, string sdp)
    {
        var answererId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Clients.User(targetUserId).SendAsync("ReceiveAnswer", answererId, sdp);
    }

    public async Task SendIceCandidate(string targetUserId, string candidate, string sdpMid, int sdpMLineIndex)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", senderId, candidate, sdpMid, sdpMLineIndex);
    }

    public async Task AcceptCall(string sessionId)
    {
        // Broadcast to caller that call is accepted
        // Here we just broadcast to the other user assuming 1-1 mappings
        await Clients.Caller.SendAsync("CallAccepted", sessionId);
    }

    public async Task RejectCall(string sessionId)
    {
        await Clients.Caller.SendAsync("CallRejected", sessionId);
    }

    public async Task EndCall()
    {
        await Clients.Others.SendAsync("CallEnded", new { SessionId = Guid.NewGuid(), Duration = 0 });
    }
}
