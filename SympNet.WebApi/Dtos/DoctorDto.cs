namespace SympNet.WebApi.Dtos;

public class DoctorDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Speciality { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? Bio { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool AcceptsOnlineConsultation { get; set; }
    public decimal ConsultationFee { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public bool IsVerified { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}


public class CreateDoctorDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Speciality { get; set; } = "Médecine Générale";
    public string? LicenseNumber { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
public class UpdateDoctorProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Speciality { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
public class UpdateDoctorPasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateDoctorDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Speciality { get; set; }
    public string? Bio { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool? AcceptsOnlineConsultation { get; set; }
    public decimal? ConsultationFee { get; set; }
}

