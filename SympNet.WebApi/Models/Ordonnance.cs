namespace SympNet.WebApi.Models;

public class Ordonnance
{
    public int Id { get; set; }
    public string OrdonnanceCode { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public int? ConsultationId { get; set; }
    public Consultation? Consultation { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string MedicationsJson { get; set; } = "[]";
    public string? Notes { get; set; }
    public OrdonnanceStatus Status { get; set; } = OrdonnanceStatus.Pending;
    public bool HasAIAlerts { get; set; } = false;
    public string? AIAlertsJson { get; set; }
    public bool DoctorConfirmed { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }
    public string? PdfPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum OrdonnanceStatus { Pending, AIChecking, AlertPending, Confirmed, Rejected }
