using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
using SympNet.WebApi.Dtos;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SympNet.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ChatController(AppDbContext db) => _db = db;

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new UnauthorizedAccessException());

        // GET: api/chat/conversations
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = CurrentUserId;
            
            var conversations = await _db.Conversations
                .Where(c => c.DoctorId == userId || c.PatientId == userId)
                .Select(c => new
                {
                    OtherUserId = c.DoctorId == userId ? c.PatientId : c.DoctorId,
                    LastMessage = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault().Content,
                    LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                    UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead)
                })
                .ToListAsync();
            
            var result = new List<ConversationResponseDto>();
            foreach (var conv in conversations)
            {
                var otherUser = await _db.Users.FindAsync(conv.OtherUserId);
                result.Add(new ConversationResponseDto
                {
                    OtherUserId = conv.OtherUserId,
                    OtherUserName = otherUser?.FullName ?? "Utilisateur",
                    LastMessage = conv.LastMessage,
                    LastMessageAt = conv.LastMessageAt,
                    UnreadCount = conv.UnreadCount,
                    IsOnline = false
                });
            }
            
            return Ok(result);
        }

        // GET: api/chat/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = CurrentUserId;
            
            var unreadCount = await _db.ChatMessages
                .Include(m => m.Conversation)
                .Where(m => !m.IsRead && 
                       ((m.Conversation.DoctorId == userId && m.SenderRole == "patient") ||
                        (m.Conversation.PatientId == userId && m.SenderRole == "doctor")))
                .CountAsync();
            
            return Ok(new { unreadCount });
        }

        // GET: api/chat/conversations/user/{otherUserId}/messages (Route corrigée)
        [HttpGet("conversations/user/{otherUserId}/messages")]
        public async Task<IActionResult> GetMessagesWithUser(Guid otherUserId, int page = 1, int pageSize = 50)
        {
            var userId = CurrentUserId;
            
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => (c.DoctorId == userId && c.PatientId == otherUserId) ||
                                           (c.DoctorId == otherUserId && c.PatientId == userId));
            
            if (conversation == null)
                return Ok(new List<object>());
            
            var messages = await _db.ChatMessages
                .Where(m => m.ConversationId == conversation.Id)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.SentAt,
                    IsMine = m.SenderId == userId,
                    m.IsRead
                })
                .ToListAsync();
            
            // Marquer comme lus
            var unreadMessages = await _db.ChatMessages
                .Where(m => m.ConversationId == conversation.Id && !m.IsRead && m.SenderId != userId)
                .ToListAsync();
            
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            
            return Ok(messages);
        }

        // POST: api/chat/conversations
        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto)
        {
            var existing = await _db.Conversations
                .FirstOrDefaultAsync(c => c.DoctorId == dto.DoctorId && c.PatientId == dto.PatientId);

            if (existing != null)
                return Ok(existing);

            var conv = new Conversation
            {
                DoctorId = dto.DoctorId,
                PatientId = dto.PatientId
            };

            _db.Conversations.Add(conv);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMessagesWithUser), new { otherUserId = dto.PatientId }, conv);
        }

        // GET: api/chat/conversations/{conversationId}/messages (conflit résolu avec "/conversation/")
        [HttpGet("conversation/{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(Guid conversationId, int page = 1, int pageSize = 30)
        {
            var userId = CurrentUserId;
            var conv = await _db.Conversations.FindAsync(conversationId);
            if (conv == null) return NotFound();

            if (conv.DoctorId != userId && conv.PatientId != userId)
                return Forbid();

            var messages = await _db.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.SenderRole,
                    m.Content,
                    m.MessageType,
                    m.FileUrl,
                    m.IsRead,
                    m.SentAt,
                    m.ReadAt
                })
                .ToListAsync();

            return Ok(messages);
        }

        // POST: api/chat/messages
        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var senderId = CurrentUserId;
            
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => (c.DoctorId == senderId && c.PatientId == dto.ReceiverId) ||
                                           (c.DoctorId == dto.ReceiverId && c.PatientId == senderId));
            
            if (conversation == null)
            {
                var sender = await _db.Users.FindAsync(senderId);
                
                if (sender?.Role == "Doctor")
                {
                    conversation = new Conversation { DoctorId = senderId, PatientId = dto.ReceiverId };
                }
                else
                {
                    conversation = new Conversation { DoctorId = dto.ReceiverId, PatientId = senderId };
                }
                
                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();
            }
            
            var message = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderId = senderId,
                SenderRole = senderId == conversation.DoctorId ? "doctor" : "patient",
                Content = dto.Content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            
            _db.ChatMessages.Add(message);
            conversation.LastMessageAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            
            return Ok(new
            {
                message.Id,
                message.Content,
                message.SentAt,
                IsMine = true,
                IsRead = false
            });
        }

        // GET: api/chat/calls/history
        [HttpGet("calls/history")]
        public async Task<IActionResult> GetCallHistory()
        {
            var userId = CurrentUserId;
            var calls = await _db.VideoCallSessions
                .Where(s => s.InitiatorId == userId || s.ReceiverId == userId)
                .OrderByDescending(s => s.StartedAt)
                .Take(50)
                .ToListAsync();

            return Ok(calls);
        }
    }

    // DTOs
    public class CreateConversationDto
    {
        public Guid DoctorId { get; set; }
        public Guid PatientId { get; set; }
    }

    public class SendMessageDto
    {
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = "";
    }
}