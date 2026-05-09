namespace SympNet.WebApi.Dtos;

public class PatientDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public string? Address { get; set; }
    public string? MedicalHistory { get; set; }
    public List<string> Allergies { get; set; } = new();
    public List<string> ChronicConditions { get; set; } = new();
    public List<string> CurrentMedications { get; set; } = new();
    public string? EmergencyContact { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int ConsultationCount { get; set; }
}

public class UpdatePatientDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodType { get; set; }
    public string? Address { get; set; }
    public string? MedicalHistory { get; set; }
    public List<string>? Allergies { get; set; }
    public List<string>? ChronicConditions { get; set; }
    public List<string>? CurrentMedications { get; set; }
    public string? EmergencyContact { get; set; }
}

public class CreatePatientDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
}
