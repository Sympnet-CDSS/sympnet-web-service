namespace SympNet.WebApi.Dtos;

public class MessageDto
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsVoice { get; set; }
    public DateTime SentAt { get; set; }
}

public class SendMessageDto
{
    public int ConsultationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsVoice { get; set; } = false;
}