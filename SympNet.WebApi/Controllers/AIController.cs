using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace SympNet.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiBaseUrl;

        public AIController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _aiBaseUrl = configuration["AIService:Url"] ?? "http://localhost:8000";
        }

        [HttpPost("diagnostic")]
        public async Task<IActionResult> GetDiagnostic([FromBody] object payload)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_aiBaseUrl}/api/v1/diagnostic", payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(result);
                }
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur de communication avec le service IA : {ex.Message}");
            }
        }

        [HttpPost("symptom-to-doctor")]
        public async Task<IActionResult> SymptomToDoctor([FromBody] object payload)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_aiBaseUrl}/api/v1/symptom-to-doctor", payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(result);
                }
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur de communication avec le service IA : {ex.Message}");
            }
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                var chatPayload = new { message = request.Message };
                var response = await _httpClient.PostAsJsonAsync($"{_aiBaseUrl}/api/v1/chat/doctor", chatPayload);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    var reply = result.TryGetProperty("reply", out var r) ? r.GetString() : "Je n'ai pas pu générer de réponse.";
                    return Ok(new { Reply = reply });
                }

                return Ok(new { Reply = "Désolé, je n'ai pas pu joindre le moteur d'analyse IA." });
            }
            catch (Exception ex)
            {
                return Ok(new { Reply = $"Erreur interne: {ex.Message}" });
            }
        }

        [HttpPost("ordonnance/check")]
        public async Task<IActionResult> CheckPrescription([FromBody] OrdonnanceCheckRequest request, [FromServices] SympNet.WebApi.Data.AppDbContext db)
        {
            try
            {
                var patientAllergies = new List<string>();
                var patientConditions = new List<string>();

                if (int.TryParse(request.PatientId, out int pid))
                {
                    var patient = await db.Patients.FindAsync(pid);
                    if (patient != null)
                    {
                        if (!string.IsNullOrEmpty(patient.Allergies))
                            patientAllergies = patient.Allergies.Split(',').Select(x => x.Trim()).ToList();
                        if (!string.IsNullOrEmpty(patient.ChronicDiseases))
                            patientConditions = patient.ChronicDiseases.Split(',').Select(x => x.Trim()).ToList();
                        if (!string.IsNullOrEmpty(patient.MedicalHistory))
                            patientConditions.Add(patient.MedicalHistory);
                    }
                }

                var aiRequest = new Dictionary<string, object>
                {
                    { "drug_names", request.Medications.Select(m => m.Name).ToList() },
                    { "patient_id", request.PatientId },
                    { "diagnosis", request.Diagnosis },
                    { "patient_allergies", patientAllergies },
                    { "patient_conditions", patientConditions }
                };

                var response = await _httpClient.PostAsJsonAsync($"{_aiBaseUrl}/api/v1/prescription-alert", aiRequest);
                if (response.IsSuccessStatusCode)
                {
                    var aiResult = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    
                    var hasInteractions = aiResult.TryGetProperty("interactions", out var iElement) && iElement.ValueKind == System.Text.Json.JsonValueKind.Array;
                    var hasContra = aiResult.TryGetProperty("contraindications", out var cElement) && cElement.ValueKind == System.Text.Json.JsonValueKind.Array;
                    
                    var alerts = new List<object>();
                    
                    if (hasInteractions)
                    {
                        foreach(var interaction in iElement.EnumerateArray())
                        {
                            alerts.Add(new { 
                                Type = "Interaction", 
                                Severity = interaction.TryGetProperty("severity", out var s) ? s.GetString() : "Medium", 
                                Message = interaction.TryGetProperty("description", out var d) ? d.GetString() : "Interaction détectée" 
                            });
                        }
                    }
                    
                    if (hasContra)
                    {
                        foreach(var contra in cElement.EnumerateArray())
                        {
                            alerts.Add(new { 
                                Type = "Contre-indication", 
                                Severity = "High", 
                                Message = contra.GetString() 
                            });
                        }
                    }

                    var result = new
                    {
                        HasAlerts = aiResult.TryGetProperty("has_interactions", out var h) && h.GetBoolean(),
                        Summary = aiResult.TryGetProperty("recommendation", out var r) ? r.GetString() : "Analyse terminée.",
                        Alerts = alerts
                    };
                    return Ok(result);
                }
                
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[AI Error] Status: {response.StatusCode}, Body: {errorBody}");
                return StatusCode((int)response.StatusCode, errorBody);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur de communication avec le service IA : {ex.Message}");
            }
        }
    }

    public class OrdonnanceCheckRequest
    {
        public string PatientId { get; set; } = "";
        public string Diagnosis { get; set; } = "";
        public List<AIMedicationItem> Medications { get; set; } = new();
    }

    public class AIMedicationItem
    {
        public string Name { get; set; } = "";
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }
}
