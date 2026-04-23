namespace SympNet.WebApi.Dtos;

public class AppointmentDto
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorSpeciality { get; set; } = string.Empty;
    public string DoctorAddress { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateAppointmentDto
{
    public int DoctorId { get; set; }
    public DateTime DateTime { get; set; }
    public string? Notes { get; set; }
}

public class UpdateAppointmentDto
{
    public DateTime? DateTime { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}