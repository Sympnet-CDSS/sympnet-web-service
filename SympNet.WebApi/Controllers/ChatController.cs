using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
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
[AllowAnonymous]
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
            
            var result = new System.Collections.Generic.List<object>();
            foreach (var conv in conversations)
            {
                var otherUser = await _db.Users.FindAsync(conv.OtherUserId);
var userName = otherUser?.FullName ?? "Utilisateur";
                result.Add(new
                {
                    conv.OtherUserId,
                    OtherUserName = otherUser?.FullName ?? "Utilisateur",
                    conv.LastMessage,
                    conv.LastMessageAt,
                    conv.UnreadCount,
                    IsOnline = false
                });
            }
            
            return Ok(result);
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
            return CreatedAtAction(nameof(GetMessages), new { conversationId = conv.Id }, conv);
        }

        // GET: api/chat/conversations/{conversationId}/messages
        [HttpGet("conversations/{conversationId}/messages")]
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
[AllowAnonymous]
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
                SentAt = DateTime.UtcNow
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