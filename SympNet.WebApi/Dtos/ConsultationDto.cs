namespace SympNet.WebApi.Dtos;

public class ConsultationDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Symptoms { get; set; }
    public string? AIDiagnosis { get; set; }
    public string? DoctorDiagnosis { get; set; }
    public string? DoctorNotes { get; set; }
    public string? AISummary { get; set; }
}

public class StartConsultationDto
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int? AppointmentId { get; set; }
    public string Type { get; set; } = "Video";
}

public class UpdateConsultationDto
{
    public string? Symptoms { get; set; }
    public string? DoctorDiagnosis { get; set; }
    public string? DoctorNotes { get; set; }
    public bool? AIConfirmed { get; set; }
}