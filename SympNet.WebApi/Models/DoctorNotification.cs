// Models/DoctorNotification.cs
namespace SympNet.WebApi.Models;

public class DoctorNotification
{
    public int      Id            { get; set; }
    public Guid     DoctorUserId  { get; set; }
    public string   Title         { get; set; } = "";
    public string   Message       { get; set; } = "";
    public int      AppointmentId { get; set; }
    public bool     IsUrgent      { get; set; }
    public bool     IsRead        { get; set; } = false;
    public DateTime SentAt        { get; set; } = DateTime.UtcNow;
}