namespace SympNet.WebApi.Dtos;

public class AdminStatsDto
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalAppointmentsToday { get; set; }
    public int TotalConsultationsThisMonth { get; set; }
    public int PendingOrdonnanceAlerts { get; set; }
    public int UnreadNotifications { get; set; }
    public Dictionary<string, int> AppointmentsByStatus { get; set; } = new();
    public Dictionary<string, int> ConsultationsBySpecialty { get; set; } = new();
}