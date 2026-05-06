using System;
using System.Collections.Generic;

namespace SympNet.WebApi.Models
{
    public class Conversation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DoctorId { get; set; }
        public Guid PatientId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMessageAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderRole { get; set; } = ""; // "doctor" | "patient"
        public string Content { get; set; } = "";
        public string MessageType { get; set; } = "text"; // "text" | "image" | "file"
        public string? FileUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation
        public Conversation? Conversation { get; set; }
    }

    public class VideoCallSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public Guid InitiatorId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Status { get; set; } = "pending"; // pending | active | ended | missed
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public int? DurationSeconds { get; set; }
    }
}
