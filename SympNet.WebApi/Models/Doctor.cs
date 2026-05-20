using System;
using System.ComponentModel.DataAnnotations;

namespace SympNet.WebApi.Models;

public class Doctor
{
    [Key]
    public int Id { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Speciality { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public int? GraduationYear { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
    public int TotalConsultations { get; set; } = 0;
    public int TotalPatients { get; set; } = 0;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}