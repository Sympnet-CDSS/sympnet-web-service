namespace SympNet.WebApi.Dtos;

public class OrdonnanceDto
{
    public int Id { get; set; }
    public string OrdonnanceCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public List<MedicationEntryDto> Medications { get; set; } = new();
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasAIAlerts { get; set; }
    public bool DoctorConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PdfPath { get; set; }
}

public class MedicationEntryDto
{
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Route { get; set; } = "oral";
    public string? Instructions { get; set; }
}

public class CreateOrdonnanceDto
{
    public int PatientId { get; set; }
    public int? ConsultationId { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public List<MedicationEntryDto> Medications { get; set; } = new();
    public string? Notes { get; set; }
}

public class ConfirmOrdonnanceDto
{
    public bool Confirm { get; set; }
    public string? DoctorComment { get; set; }
}