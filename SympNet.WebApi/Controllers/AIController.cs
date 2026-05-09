using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SympNet.WebApi.Hubs;
using SympNet.WebApi.Services;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;

    public AIController(IAIService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("diagnose")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> RunDiagnostic([FromBody] AIDiagnosticRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Invalid request payload" });

        var result = await _aiService.RunDiagnosticAsync(request);
        
        if (result == null)
            return StatusCode(500, new { message = "Error communicating with AI service (MS1)." });

        return Ok(result);
    }

    [HttpPost("ordonnance/check")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CheckOrdonnance([FromBody] OrdonnanceCheckRequest request, [FromServices] IHubContext<NotificationHub> hubContext)
    {
        if (request == null)
            return BadRequest(new { message = "Invalid request payload" });

        var result = await _aiService.CheckOrdonnanceAsync(request);

        if (result == null)
            return StatusCode(500, new { message = "Error verifying ordonnance with AI service (MS1)." });

        // Push SignalR -> médecin (décision finale)
        if (result.HasAlerts)
        {
            var doctorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (doctorId != null)
            {
                await hubContext.Clients.Group($"user_{doctorId}").SendAsync("ReceiveAlert", result);
            }
        }

        return Ok(result);
    }

    [HttpPost("symptom-to-doctor")]
    [AllowAnonymous] // Maybe patient app needs this without full auth, or maybe Patient role
    public async Task<IActionResult> FindDoctors([FromBody] SymptomDoctorRequest request)
    {
        if (string.IsNullOrEmpty(request.Symptoms))
            return BadRequest(new { message = "Symptoms are required." });

        var result = await _aiService.FindDoctorsBySymptomsAsync(request.Symptoms, request.Lat, request.Lng);
        return Ok(result);
    }

    [HttpPost("chat")]
    [AllowAnonymous] // Or Authorize, depending on usage. If admin/doctor use it, it can be authorize. Let's make it available to authenticated users.
    [Authorize]
    public async Task<IActionResult> Chat([FromBody] AIChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message is required." });

        var result = await _aiService.ChatAsync(request);
        return Ok(result);
    }
}

public class SymptomDoctorRequest
{
    public string Symptoms { get; set; } = "";
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}
