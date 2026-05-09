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
            
            var alert = new OrdonnanceAlert
            {
                HasAlerts = json.TryGetProperty("has_alerts", out var ha) && ha.GetBoolean(),
                Summary = json.TryGetProperty("summary", out var summ) ? summ.GetString() ?? "" : "",
                Alerts = new List<AlertItem>()
            };
            
            if (json.TryGetProperty("alerts", out var alertsArr) && alertsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in alertsArr.EnumerateArray())
                {
                    var item = new AlertItem
                    {
                        Type = a.TryGetProperty("type", out var type) ? type.GetString() ?? "" : "",
                        Severity = a.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "" : "",
                        Message = a.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "",
                        InvolvedMedications = new List<string>()
                    };
                    
                    if (a.TryGetProperty("involved_medications", out var invMeds) && invMeds.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var m in invMeds.EnumerateArray())
                        {
                            var mStr = m.GetString();
                            if (!string.IsNullOrEmpty(mStr)) item.InvolvedMedications.Add(mStr);
                        }
                    }
                    alert.Alerts.Add(item);
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
            
            // MS1 returns NearbyDoctorsResponse with nearby_doctors array
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
            var diagnosticRequest = new AIDiagnosticRequest
            {
                patient_id = "chat_user",
                text = request.Message,
                allergies = new List<string>()
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/diagnostic", diagnosticRequest);
            if (response.IsSuccessStatusCode)
            {
                var diagStr = await response.Content.ReadAsStringAsync();
                var reply = "Analyse terminée.";
                
                try 
                {
                    var root = JsonDocument.Parse(diagStr).RootElement;
                    var sb = new StringBuilder();

                    sb.AppendLine("<div class='ai-response' style='font-family: inherit; font-size: 14px;'>");

                    // 1. Symptômes
                    var symptomNode = root.TryGetProperty("symptom_analysis", out var symptomAnalysis) ? symptomAnalysis : root;
                    if (symptomNode.TryGetProperty("extracted_symptoms", out var symptoms) && symptoms.ValueKind == JsonValueKind.Array)
                    {
                        sb.AppendLine("<div style='margin-bottom: 12px;'>");
                        sb.AppendLine("<strong style='color: #0D9488;'>Symptômes</strong><br/>");
                        sb.AppendLine("<ul style='margin: 4px 0 0 0; padding-left: 20px; color: #374151;'>");
                        foreach (var symp in symptoms.EnumerateArray())
                        {
                            sb.AppendLine($"<li>{symp.GetString()}</li>");
                        }
                        sb.AppendLine("</ul></div>");
                    }

                    // 2. Diagnostic IA
                    string diagnosis = "Non déterminé";
                    if (root.TryGetProperty("hypotheses", out var hypNode) && hypNode.TryGetProperty("generated_hypotheses", out var genHyp) && genHyp.ValueKind == JsonValueKind.Array && genHyp.GetArrayLength() > 0)
                    {
                        var firstHyp = genHyp[0];
                        if (firstHyp.TryGetProperty("diagnosis", out var diagElement))
                        {
                            diagnosis = diagElement.GetString() ?? "Non déterminé";
                        }
                    }
                    else if (symptomNode.TryGetProperty("specialty", out var s) && s.ValueKind == JsonValueKind.String)
                    {
                        diagnosis = s.GetString() ?? "Non déterminé";
                    }
                    
                    sb.AppendLine("<div style='margin-bottom: 12px;'>");
                    sb.AppendLine("<strong style='color: #0D9488;'>Diagnostic IA</strong><br/>");
                    sb.AppendLine($"<div style='background: #E6F8F7; color: #0D6E6A; padding: 4px 10px; border-radius: 6px; display: inline-block; font-weight: 600; margin-top: 4px;'>{diagnosis.ToUpper()}</div>");
                    sb.AppendLine("</div>");

                    // 3. Score de confiance
                    double confScore = 0;
                    if (root.TryGetProperty("confidence", out var confNode) && confNode.TryGetProperty("final_score", out var finalScore))
                    {
                        confScore = finalScore.GetDouble() * 100;
                    }
                    else if (symptomNode.TryGetProperty("specialty_confidence", out var c))
                    {
                        confScore = c.GetDouble() * 100;
                    }
                    sb.AppendLine("<div style='margin-bottom: 12px;'>");
                    sb.AppendLine("<strong style='color: #0D9488;'>Score de confiance</strong><br/>");
                    sb.AppendLine($"<span style='font-weight: 600; color: #374151;'>{Math.Round(confScore)}%</span>");
                    sb.AppendLine("</div>");

                    // 4. Recommandations / Explications
                    string explanation = "";
                    if (root.TryGetProperty("explanation", out var expNode) && expNode.ValueKind == JsonValueKind.String)
                    {
                        explanation = expNode.GetString() ?? "";
                    }
                    else if (root.TryGetProperty("hypotheses", out var hNode) && hNode.TryGetProperty("generated_hypotheses", out var gHyp) && gHyp.ValueKind == JsonValueKind.Array && gHyp.GetArrayLength() > 0)
                    {
                        if (gHyp[0].TryGetProperty("explanation", out var expl))
                        {
                            explanation = expl.GetString() ?? "";
                        }
                    }

                    if (!string.IsNullOrEmpty(explanation))
                    {
                        sb.AppendLine("<div style='margin-bottom: 12px;'>");
                        sb.AppendLine("<strong style='color: #0D9488;'>Recommandations / Explications</strong><br/>");
                        // Split by period to make it look like a list if it's a paragraph
                        var sentences = explanation.Split('.', StringSplitOptions.RemoveEmptyEntries);
                        if (sentences.Length > 1)
                        {
                            sb.AppendLine("<ul style='margin: 4px 0 0 0; padding-left: 20px; color: #374151;'>");
                            foreach (var sentence in sentences)
                            {
                                if (sentence.Trim().Length > 3)
                                    sb.AppendLine($"<li>{sentence.Trim()}</li>");
                            }
                            sb.AppendLine("</ul>");
                        }
                        else
                        {
                            sb.AppendLine($"<div style='color: #374151; margin-top: 4px;'>{explanation}</div>");
                        }
                        sb.AppendLine("</div>");
                    }

                    sb.AppendLine("</div>");
                    
                    if (sb.Length > 200) { 
                        reply = sb.ToString();
                    }
                }
                catch 
                {
                    // Fallback if parsing fails
                    var diagResult = JsonSerializer.Deserialize<AIDiagnosticResponse>(diagStr);
                    if (diagResult?.explanation != null) reply = diagResult.explanation.ToString() ?? reply;
                }

                return new AIChatResponse { Reply = reply };
            }
            
            return new AIChatResponse { Reply = "Je suis l'IA médicale. (Réponse simulée, MS1 non connecté)." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur chat IA");
            return new AIChatResponse { Reply = "Je rencontre actuellement des difficultés à me connecter à mon serveur d'intelligence artificielle sur le port 8001." };
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
    // MS1 returns DiagnosticPipelineResponse
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
    public string Type { get; set; } = ""; // allergy, interaction, contraindication
    public string Severity { get; set; } = ""; // high, medium, low
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