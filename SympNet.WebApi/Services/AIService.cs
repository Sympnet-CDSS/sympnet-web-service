using System.Text;
using System.Text.Json;

namespace SympNet.WebApi.Services;

public interface IAIService
{
    Task<AIDiagnosticResponse?> RunDiagnosticAsync(AIDiagnosticRequest request);
    Task<OrdonnanceAlert?> CheckOrdonnanceAsync(OrdonnanceCheckRequest request);
    Task<List<DoctorSuggestion>> FindDoctorsBySymptomsAsync(string symptoms, double? lat, double? lng);
    Task<string?> TranscribeAudioAsync(byte[] audioData);
    Task<AIChatResponse?> ChatAsync(AIChatRequest request);
}

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;

    public AIService(HttpClient httpClient, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AIDiagnosticResponse?> RunDiagnosticAsync(AIDiagnosticRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/diagnostic", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AIDiagnosticResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur appel diagnostic IA");
            return null;
        }
    }

    public async Task<OrdonnanceAlert?> CheckOrdonnanceAsync(OrdonnanceCheckRequest request)
    {
        try
        {
            var ms1Request = new 
            {
                drug_names = request.Medications.Select(m => m.Name).ToList(),
                patient_id = request.PatientId ?? "anonymous",
                patient_allergies = request.PatientAllergies ?? new List<string>()
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/prescription-alert", ms1Request);
            response.EnsureSuccessStatusCode();
            
            var resultStr = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(resultStr).RootElement;
            
            var hasInteractions = json.TryGetProperty("has_interactions", out var hi) && hi.GetBoolean();
            var hasContraindications = json.TryGetProperty("contraindications", out var contras) && contras.GetArrayLength() > 0;
            
            var alert = new OrdonnanceAlert
            {
                HasAlerts = hasInteractions || hasContraindications,
                Summary = json.TryGetProperty("recommendation", out var rec) ? rec.GetString() ?? "" : "",
                Alerts = new List<AlertItem>()
            };
            
            if (json.TryGetProperty("interactions", out var interactionsArr) && interactionsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var i in interactionsArr.EnumerateArray())
                {
                    var drug1 = i.TryGetProperty("drug_1", out var d1) ? d1.GetString() ?? "" : "";
                    var drug2 = i.TryGetProperty("drug_2", out var d2) ? d2.GetString() ?? "" : "";
                    alert.Alerts.Add(new AlertItem
                    {
                        Type = "Interaction Médicamenteuse",
                        Severity = i.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "modérée" : "modérée",
                        Message = i.TryGetProperty("description", out var msg) ? msg.GetString() ?? "" : "",
                        InvolvedMedications = new List<string> { drug1, drug2 }
                    });
                }
            }

            if (json.TryGetProperty("contraindications", out var contraArr) && contraArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in contraArr.EnumerateArray())
                {
                    alert.Alerts.Add(new AlertItem
                    {
                        Type = "Contre-indication",
                        Severity = "critique",
                        Message = c.GetString() ?? ""
                    });
                }
            }

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur vérification ordonnance IA");
            return null;
        }
    }

    public async Task<List<DoctorSuggestion>> FindDoctorsBySymptomsAsync(string symptoms, double? lat, double? lng)
    {
        try
        {
            var payload = new { text = symptoms, location_lat = lat, location_lng = lng };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/symptom-to-doctor", payload);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            
            if (result.TryGetProperty("nearby_doctors", out var doctorsArray))
            {
                return JsonSerializer.Deserialize<List<DoctorSuggestion>>(doctorsArray.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DoctorSuggestion>();
            }
            return new List<DoctorSuggestion>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur recherche médecins IA");
            return new List<DoctorSuggestion>();
        }
    }

    public async Task<string?> TranscribeAudioAsync(byte[] audioData)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "audio", "recording.wav");

            var response = await _httpClient.PostAsync("/api/v1/voice/transcribe", content);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("transcript").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur transcription audio");
            return null;
        }
    }

    public async Task<AIChatResponse?> ChatAsync(AIChatRequest request)
    {
        try
        {
            var chatPayload = new { message = request.Message };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/chat/doctor", chatPayload);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AIChatResponse>();
                return new AIChatResponse { Reply = result?.Reply ?? "Je n'ai pas pu générer de réponse." };
            }
            
            return new AIChatResponse { Reply = "Je suis l'IA médicale. (Erreur de communication avec le modèle expert)." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur chat IA");
            return new AIChatResponse { Reply = "Je rencontre actuellement des difficultés à me connecter à mon serveur d'intelligence artificielle sur le port 8000." };
        }
    }
}

// DTOs pour AI
public class AIDiagnosticRequest
{
    public string patient_id { get; set; } = "anonymous";
    public string? text { get; set; }
    public string? audio_base64 { get; set; }
    public string? patient_history { get; set; }
    public List<string> allergies { get; set; } = new();
}

public class AIDiagnosticResponse
{
    //  returns DiagnosticPipelineResponse
    public object? symptom_analysis { get; set; }
    public object? hypotheses { get; set; }
    public object? validation { get; set; }
    public object? explanation { get; set; }
    public object? confidence { get; set; }
    public double processing_time_ms { get; set; }
}

public class DoctorSuggestion
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Specialty { get; set; } = "";
    public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceKm { get; set; }
    public double Rating { get; set; }
    public string Phone { get; set; } = "";
}

public class OrdonnanceCheckRequest
{
    public string DoctorId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string PatientDob { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public List<MedicationEntry> Medications { get; set; } = new();
    public string? Notes { get; set; }
    public List<string> PatientAllergies { get; set; } = new();
    public List<string> PatientConditions { get; set; } = new();
    public List<string> CurrentMedications { get; set; } = new();
}

public class MedicationEntry
{
    public string Name { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Frequency { get; set; } = "";
    public string Duration { get; set; } = "";
    public string Route { get; set; } = "";
    public string? Instructions { get; set; }
}

public class OrdonnanceAlert
{
    public bool HasAlerts { get; set; }
    public List<AlertItem> Alerts { get; set; } = new();
    public string Summary { get; set; } = "";
}

public class AlertItem
{
    public string Type { get; set; } = ""; 
    public string Severity { get; set; } = ""; 
    public string Message { get; set; } = "";
    public List<string> InvolvedMedications { get; set; } = new();
}

public class AIChatRequest
{
    public string Message { get; set; } = "";
}

public class AIChatResponse
{
    public string Reply { get; set; } = "";
}