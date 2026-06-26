namespace SympNet.WebApi.Models;

public class PatientNotification
{
    public int      Id            { get; set; }
    public Guid     PatientUserId { get; set; }
    public string   Title         { get; set; } = "";
    public string   Message       { get; set; } = "";
    public int      AppointmentId { get; set; }
    public string   Status        { get; set; } = "";
    public bool     IsRead        { get; set; } = false;
    public DateTime SentAt        { get; set; } = DateTime.UtcNow;
}