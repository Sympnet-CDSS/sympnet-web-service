namespace SympNet.WebApi;

public class ConversationResponseDto
{
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = "";
    public string? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
}