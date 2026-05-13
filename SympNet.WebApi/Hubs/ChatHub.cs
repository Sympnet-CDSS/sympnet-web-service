using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SympNet.WebApi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly SympNet.WebApi.Data.AppDbContext _db;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public ChatHub(SympNet.WebApi.Data.AppDbContext db, IHubContext<NotificationHub> notificationHub)
    {
        _db = db;
        _notificationHub = notificationHub;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
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
    var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Context.User?.FindFirst("sub")?.Value;
    
    Console.WriteLine($"[SignalR] SendMessage from {senderIdStr} for consultation {consultationId}");

    if (!Guid.TryParse(senderIdStr, out var senderId)) 
    {
        Console.WriteLine("[SignalR] Invalid SenderId GUID");
        return;
    }

    var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Utilisateur";
    var senderRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "patient";

    Guid receiverId = Guid.Empty;

    if (Guid.TryParse(consultationId, out var convOrPartnerId))
    {
        var conv = _db.Conversations.FirstOrDefault(c =>
            c.Id == convOrPartnerId ||
            (c.DoctorId  == senderId && c.PatientId == convOrPartnerId) ||
            (c.DoctorId  == convOrPartnerId && c.PatientId == senderId));

        if (conv == null)
        {
            Console.WriteLine($"[SignalR] Conversation not found for {convOrPartnerId}. Creating new one.");
            var senderUser = _db.Users.Find(senderId);
            conv = senderUser?.Role == "Doctor"
                ? new SympNet.WebApi.Models.Conversation { DoctorId = senderId, PatientId = convOrPartnerId }
                : new SympNet.WebApi.Models.Conversation { DoctorId = convOrPartnerId, PatientId = senderId };
            _db.Conversations.Add(conv);
            _db.SaveChanges();
        }

        var chatMsg = new SympNet.WebApi.Models.ChatMessage
        {
            ConversationId = conv.Id,
            SenderId       = senderId,
            SenderRole     = senderId == conv.DoctorId ? "doctor" : "patient",
            Content        = message,
            SentAt         = DateTime.UtcNow
        };
        _db.ChatMessages.Add(chatMsg);
        conv.LastMessageAt = DateTime.UtcNow;

        // ✅ receiverId = l'autre participant
        receiverId = senderId == conv.DoctorId ? conv.PatientId : conv.DoctorId;

        // ✅ CORRECTION — isDoctor était inversé
        // senderId == conv.DoctorId → l'expéditeur est le docteur
        // donc le receiver est le patient → notification patient
        bool senderIsDoctor = senderId == conv.DoctorId;

        if (senderIsDoctor)
        {
            // ✅ Docteur envoie → notifier le patient
            _db.PatientNotifications.Add(new SympNet.WebApi.Models.PatientNotification
            {
                PatientUserId = receiverId,
                Title         = $"Nouveau message de {senderName}",
                Message       = message,
                SentAt        = DateTime.UtcNow,
                IsRead        = false,
                Status        = "New"
            });
        }
        else
        {
            // ✅ Patient envoie → notifier le docteur
            _db.DoctorNotifications.Add(new SympNet.WebApi.Models.DoctorNotification
            {
                DoctorUserId = receiverId,
                Title        = $"Nouveau message de {senderName}",
                Message      = message,
                SentAt       = DateTime.UtcNow,
                IsRead       = false
            });
        }

        _db.SaveChanges();

        // ✅ Notifier le tableau de bord (format attendu par Dashboard.razor)
        var notifData = new
        {
            id            = 0,
            title         = $"Nouveau message de {senderName}",
            message       = message,
            appointmentId = 0,
            isUrgent      = false,
            sentAt        = DateTime.UtcNow,
            type          = "CHAT"
        };

        await _notificationHub.Clients.Group($"user_{receiverId}").SendAsync("ReceiveNotification", notifData);
        await _notificationHub.Clients.Group($"doctor_user_{receiverId}").SendAsync("ReceiveNotification", notifData);

        // ✅ Broadcast UNIQUE au groupe de la conversation
        // Le client doit s'assurer de rejoindre "consultation_{conv.Id}"
        await Clients.Group($"consultation_{conv.Id}").SendAsync("ReceiveMessage", senderIdStr, senderName, senderRole, message, isVoice, DateTime.UtcNow.ToString("o"));
        
        // ✅ Optionnel: notifier spécifiquement le destinataire s'il n'est pas dans le salon
        await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", senderIdStr, senderName, senderRole, message, isVoice, DateTime.UtcNow.ToString("o"));
    }
    else
    {
        // Cas d'un consultationId qui n'est pas un GUID (ex: salon nommé manuellement)
        await Clients.Group($"consultation_{consultationId}").SendAsync("ReceiveMessage", senderIdStr, senderName, senderRole, message, isVoice, DateTime.UtcNow.ToString("o"));
    }
}

    //  WebRTC Signaling 

    public async Task SendOffer(string targetUserId, string sdp)
    {
        var callerId   = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var callerName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        var sessionId  = Guid.NewGuid().ToString();

        Console.WriteLine($"[SignalR] SendOffer from {callerId} to {targetUserId}. Session: {sessionId}");

        await Clients.User(targetUserId).SendAsync("IncomingCall", sessionId, callerId, callerName, sdp);

        // ✅ Notifier également via le Hub de Notifications pour faire "sonner" le dashboard/mobile
        var callNotif = new
        {
            id            = 0,
            title         = $"Appel entrant de {callerName}",
            message       = "Téléconsultation lancée",
            appointmentId = 0,
            isUrgent      = true,
            sentAt        = DateTime.UtcNow,
            type          = "VIDEO_CALL",
            sessionId     = sessionId,
            callerId      = callerId
        };
        await _notificationHub.Clients.Group($"user_{targetUserId}").SendAsync("ReceiveNotification", callNotif);
        await _notificationHub.Clients.Group($"doctor_user_{targetUserId}").SendAsync("ReceiveNotification", callNotif);
    }

    public async Task AcceptCall(string targetUserId, string sessionId)
    {
        await Clients.User(targetUserId).SendAsync("CallAccepted", sessionId);
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

    public async Task MarkAsRead(string conversationId)
    {
        if (Guid.TryParse(conversationId, out var convId))
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;
            
            if (Guid.TryParse(userIdStr, out var userId))
            {
                var messages = _db.ChatMessages
                    .Where(m => m.ConversationId == convId && m.SenderId != userId && !m.IsRead)
                    .ToList();

                foreach (var msg in messages) msg.IsRead = true;
                _db.SaveChanges();

                // Notifier l'autre utilisateur que ses messages ont été lus
                var conv = _db.Conversations.Find(convId);
                if (conv != null)
                {
                    var otherUserId = userId == conv.DoctorId ? conv.PatientId : conv.DoctorId;
                    await Clients.User(otherUserId.ToString()).SendAsync("MessagesRead", convId.ToString());
                }
            }
        }
    }

    public async Task RejectCall(string targetUserId, string sessionId)
    {
        await Clients.User(targetUserId).SendAsync("CallRejected", sessionId);
    }

    public async Task EndCall(string targetUserId, string sessionId)
    {
        await Clients.User(targetUserId).SendAsync("CallEnded", sessionId);
    }
}