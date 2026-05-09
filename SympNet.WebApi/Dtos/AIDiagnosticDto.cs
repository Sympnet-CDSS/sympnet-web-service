namespace SympNet.WebApi.Dtos;

public class AIDiagnosticRequestDto
{
    public string SymptomsText { get; set; } = string.Empty;
    public string? PatientHistory { get; set; }
    public List<string> CurrentMedications { get; set; } = new();
    public List<string> Allergies { get; set; } = new();
    public List<string> Conditions { get; set; } = new();
    public bool IsRareDiseaseSuspected { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public record AIDiagnosticResponseDto(
    object SymptomAnalysis,
    object Hypotheses,
    object Validation,
    object Explanation,
    object Confidence,
    double ProcessingTimeMs
);