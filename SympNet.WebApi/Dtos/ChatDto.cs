namespace SympNet.WebApi.Dtos;

public class ConversationResponseDto
{
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = "";
    public string? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
}

public class MessageResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
    public DateTime SentAt { get; set; }
    public bool IsMine { get; set; }
    public bool IsRead { get; set; }
}

public class SendMessageRequestDto
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = "";
}