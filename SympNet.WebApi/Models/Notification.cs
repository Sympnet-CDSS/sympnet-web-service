namespace SympNet.WebApi.Models;

public class Notification
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    AppointmentConfirmed, AppointmentCancelled, AppointmentReminder,
    OrdonnanceAlert, OrdonnanceReady,
    ConsultationStarted, ConsultationEnded,
    NewMessage, AIAlert, General
}
