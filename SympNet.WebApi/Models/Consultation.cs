using System.ComponentModel.DataAnnotations;

namespace SympNet.WebApi.Models;

public class Consultation
{
    [Key]
    public int Id { get; set; }
    
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string Status { get; set; } = "En attente"; // En attente, Terminée, Annulée
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
}