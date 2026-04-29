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
    [Authorize]  // Garder l'authentification
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;

        public ChatHub(AppDbContext db)
        {
            _db = db;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string receiverId, string content)
        {
            var senderId = GetUserId();
            var targetId = Guid.Parse(receiverId);
            
            // Trouver ou créer la conversation
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => (c.DoctorId == senderId && c.PatientId == targetId) ||
                                           (c.DoctorId == targetId && c.PatientId == senderId));
            
            if (conversation == null)
            {
                var sender = await _db.Users.FindAsync(senderId);
                if (sender?.Role == "Doctor")
                {
                    conversation = new Conversation { DoctorId = senderId, PatientId = targetId };
                }
                else
                {
                    conversation = new Conversation { DoctorId = targetId, PatientId = senderId };
                }
                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();
            }
            
            var message = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderId = senderId,
                SenderRole = senderId == conversation.DoctorId ? "doctor" : "patient",
                Content = content,
                SentAt = DateTime.UtcNow
            };
            
            _db.ChatMessages.Add(message);
            conversation.LastMessageAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            
            // Envoyer au destinataire
            await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = message.SenderId,
                content = message.Content,
                sentAt = message.SentAt
            });
        }

        public async Task UserTyping(string receiverId)
        {
            await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", new
            {
                userId = GetUserId(),
                isTyping = true
            });
        }

        public async Task UserStoppedTyping(string receiverId)
        {
            await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", new
            {
                userId = GetUserId(),
                isTyping = false
            });
        }

        private Guid GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? Context.User?.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
                throw new HubException("Utilisateur non authentifié");
            
            return Guid.Parse(userIdClaim);
        }
    }
}  