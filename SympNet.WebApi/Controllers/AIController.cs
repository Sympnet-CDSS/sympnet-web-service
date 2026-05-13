using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

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
    }
}
