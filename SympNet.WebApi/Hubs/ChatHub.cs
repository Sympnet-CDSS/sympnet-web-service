using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SympNet.WebApi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($" SignalR connected: {userId}");
        await base.OnConnectedAsync();
    }

    //  Consultation rooms 

    public async Task JoinConsultation(string consultationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"consultation_{consultationId}");
        await Clients.Group($"consultation_{consultationId}")
            .SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task LeaveConsultation(string consultationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"consultation_{consultationId}");
        await Clients.Group($"consultation_{consultationId}")
            .SendAsync("UserLeft", Context.UserIdentifier);
    }

    //  Chat 

    public async Task SendMessage(string consultationId, string message, bool isVoice = false)
    {
        var senderId   = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        var senderRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        //  Paramètres séparés — pas d'objet anonyme
        // Android : hubConnection.on("ReceiveMessage", ..., String, String, String, String, Boolean, String)
        await Clients.Group($"consultation_{consultationId}")
            .SendAsync("ReceiveMessage",
                senderId,
                senderName,
                senderRole,
                message,
                isVoice,
                DateTime.UtcNow.ToString("o"));
    }

    public async Task SendTyping(string consultationId, bool isTyping)
    {
        var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        //  Paramètres séparés
        // Android : hubConnection.on("UserTyping", ..., String, Boolean)
        await Clients.OthersInGroup($"consultation_{consultationId}")
            .SendAsync("UserTyping", senderName, isTyping);
    }

    //  WebRTC Signaling 

    public async Task SendOffer(string targetUserId, string sdp)
    {
        var callerId   = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var callerName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        var sessionId  = Guid.NewGuid().ToString();

        //  Un seul événement avec paramètres séparés
        // Android : hubConnection.on("IncomingCall", ..., String, String, String, String)
        await Clients.User(targetUserId)
            .SendAsync("IncomingCall", sessionId, callerId, callerName, sdp);
    }

    public async Task SendAnswer(string targetUserId, string sdp)
    {
        var answererId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //  Notifie le caller (targetUserId) que l'appel est accepté + SDP answer
        // Android : hubConnection.on("CallAccepted", ..., String, String)
        await Clients.User(targetUserId)
            .SendAsync("CallAccepted", answererId, sdp);
    }

    public async Task SendIceCandidate(string targetUserId, string candidate,
                                       string sdpMid, int sdpMLineIndex)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Android : hubConnection.on("ReceiveIceCandidate", ..., String, String, String, Integer)
        await Clients.User(targetUserId)
            .SendAsync("ReceiveIceCandidate", senderId, candidate, sdpMid, sdpMLineIndex);
    }

    public async Task RejectCall(string targetUserId, string sessionId)
    {
        //  Notifie le caller (targetUserId), pas le Caller lui-même
        // Android : hubConnection.on("CallRejected", ..., String)
        await Clients.User(targetUserId)
            .SendAsync("CallRejected", sessionId);
    }

    public async Task EndCall(string targetUserId, string sessionId)
    {
        //  Notifie uniquement l'autre participant
        // Android : hubConnection.on("CallEnded", ..., String)
        await Clients.User(targetUserId)
            .SendAsync("CallEnded", sessionId);
    }
    
}