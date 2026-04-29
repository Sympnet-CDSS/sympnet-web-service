using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SympNet.WebApi.Hubs
{
    [Authorize]
    public class WebRTCHub : Hub
    {
        private readonly AppDbContext _db;

        public WebRTCHub(AppDbContext db)
        {
            _db = db;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"rtc_{userId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"rtc_{userId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task InitiateCall(string targetUserId, string conversationId)
        {
            var callerId = GetUserId();
            var targetId = Guid.Parse(targetUserId);
            var convId = Guid.Parse(conversationId);

            var session = new VideoCallSession
            {
                ConversationId = convId,
                InitiatorId = callerId,
                ReceiverId = targetId,
                Status = "pending",
                StartedAt = DateTime.UtcNow
            };

            _db.VideoCallSessions.Add(session);
            await _db.SaveChangesAsync();

            await Clients.Group($"rtc_{targetUserId}").SendAsync("IncomingCall", new
            {
                sessionId = session.Id,
                callerId,
                conversationId = convId
            });
        }

        public async Task AcceptCall(string sessionId)
        {
            var session = await _db.VideoCallSessions.FindAsync(Guid.Parse(sessionId));
            if (session == null) throw new HubException("Session not found");

            session.Status = "active";
            await _db.SaveChangesAsync();

            await Clients.Group($"rtc_{session.InitiatorId}").SendAsync("CallAccepted", new
            {
                sessionId = session.Id
            });
        }

        public async Task RejectCall(string sessionId)
        {
            var session = await _db.VideoCallSessions.FindAsync(Guid.Parse(sessionId));
            if (session == null) throw new HubException("Session not found");

            session.Status = "missed";
            session.EndedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await Clients.Group($"rtc_{session.InitiatorId}").SendAsync("CallRejected", new
            {
                sessionId = session.Id
            });
        }

        public async Task EndCall(string sessionId)
        {
            var session = await _db.VideoCallSessions.FindAsync(Guid.Parse(sessionId));
            if (session == null) throw new HubException("Session not found");

            session.Status = "ended";
            session.EndedAt = DateTime.UtcNow;
            session.DurationSeconds = (int)(session.EndedAt.Value - session.StartedAt).TotalSeconds;
            await _db.SaveChangesAsync();

            var peerId = GetUserId() == session.InitiatorId ? session.ReceiverId : session.InitiatorId;
            await Clients.Group($"rtc_{peerId}").SendAsync("CallEnded", new
            {
                sessionId = session.Id,
                duration = session.DurationSeconds
            });
        }

        public async Task SendOffer(string targetUserId, string sdp)
        {
            await Clients.Group($"rtc_{targetUserId}").SendAsync("ReceiveOffer", new
            {
                fromUserId = GetUserId(),
                sdp
            });
        }

        public async Task SendAnswer(string targetUserId, string sdp)
        {
            await Clients.Group($"rtc_{targetUserId}").SendAsync("ReceiveAnswer", new
            {
                fromUserId = GetUserId(),
                sdp
            });
        }

        public async Task SendIceCandidate(string targetUserId, string candidate, string sdpMid, int sdpMLineIndex)
        {
            await Clients.Group($"rtc_{targetUserId}").SendAsync("ReceiveIceCandidate", new
            {
                fromUserId = GetUserId(),
                candidate,
                sdpMid,
                sdpMLineIndex
            });
        }

        private Guid GetUserId()
        {
            var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? Context.User?.FindFirst("sub")?.Value;
            return id != null ? Guid.Parse(id) : throw new HubException("Unauthorized");
        }
    }
}