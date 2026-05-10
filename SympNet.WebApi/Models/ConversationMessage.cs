namespace SympNet.WebApi.Models;

public class ConversationMessage
{
    public int Id { get; set; }
    public int ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public string SenderId { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty; // Doctor / Patient / AI
    public string Content { get; set; } = string.Empty;
    public bool IsVoice { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
