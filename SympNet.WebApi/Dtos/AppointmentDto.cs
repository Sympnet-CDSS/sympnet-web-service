namespace SympNet.WebApi.Dtos;

public class AppointmentDto
{
    public int      Id               { get; set; }
    public Guid     PatientId        { get; set; } 
    public string   PatientName      { get; set; } = string.Empty;
    public string   PatientEmail     { get; set; } = string.Empty;
    public string   PatientPhone     { get; set; } = string.Empty;
    public int      PatientAge       { get; set; }
    public string   PatientLocation  { get; set; } = string.Empty;
    public string   PatientConditions { get; set; } = string.Empty;
    public string   PatientGender    { get; set; } = string.Empty;
    public string   PatientPhotoUrl  { get; set; } = string.Empty;
    public int      DoctorId         { get; set; }
    public string   DoctorName       { get; set; } = string.Empty;
    public string   DoctorSpeciality { get; set; } = string.Empty;
    public string   DoctorAddress    { get; set; } = string.Empty;
    public DateTime DateTime         { get; set; }
    public TimeOnly StartTime        { get; set; }
    public TimeOnly EndTime          { get; set; }
    public string   Status           { get; set; } = string.Empty;
    public string   Type             { get; set; } = string.Empty;
    public string?  Reason           { get; set; }
    public string?  Notes            { get; set; }
    public bool     IsUrgent         { get; set; }
    public bool     IsPaid           { get; set; }
    public DateTime CreatedAt        { get; set; }
}

public class CreateAppointmentDto
{
    public int      DoctorId     { get; set; }
    public DateTime DateTime     { get; set; }
    public TimeOnly StartTime    { get; set; }
    public string   Type         { get; set; } = "InPerson";
    public string?  Reason       { get; set; }
    public string?  Notes        { get; set; }
    public bool     IsUrgent     { get; set; }
    public string?  PatientEmail { get; set; }
    public int      Duration     { get; set; }
}

public class UpdateAppointmentStatusDto
{
    public string  Status { get; set; } = string.Empty;
    public string? Notes  { get; set; }  
    public string? Reason { get; set; }
}