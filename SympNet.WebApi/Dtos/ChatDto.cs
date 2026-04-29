namespace SympNet.WebApi.Dtos;

public class MessageDto
{
    public int Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public string? AttachmentName { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsMine { get; set; }
}

public class ConversationDto
{
    public int Id { get; set; }
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserRole { get; set; } = string.Empty;
    public string? OtherUserAvatar { get; set; }
    public string? OtherUserSpeciality { get; set; }
    public string? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
}

public class SendMessageDto
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
}

public class QuickReplyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class TypingDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
}

public class WebRTCOfferDto
{
    public Guid CallId { get; set; }
    public Guid CallerId { get; set; }
    public string CallerName { get; set; } = string.Empty;
    public string CallType { get; set; } = "video";
    public object Offer { get; set; } = null!;
}

public class WebRTCAnswerDto
{
    public Guid CallId { get; set; }
    public object Answer { get; set; } = null!;
}

public class WebRTCIceCandidateDto
{
    public Guid CallId { get; set; }
    public object Candidate { get; set; } = null!;
}