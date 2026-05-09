namespace SympNet.WebApi.Dtos;

public class AppointmentDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? DoctorNotes { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class CreateAppointmentDto
{
    public int DoctorId { get; set; }
    public DateTime DateTime { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Type { get; set; } = "InPerson";
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
public class UpdateAppointmentStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
