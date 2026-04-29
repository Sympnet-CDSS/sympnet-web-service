using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor")]
public class ConsultationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConsultationsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException();
        return Guid.Parse(userIdClaim);
    }

    [HttpGet]
    public async Task<IActionResult> GetConsultations()
    {
        var doctorId = GetCurrentUserId();
        
        var consultations = await _db.Consultations
            .Where(c => c.DoctorId == doctorId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.PatientName,
                c.PatientEmail,
                c.Diagnosis,
                c.ConfidenceScore,
                c.Status,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(consultations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetConsultation(int id)
    {
        var doctorId = GetCurrentUserId();
        
        var consultation = await _db.Consultations
            .Where(c => c.Id == id && c.DoctorId == doctorId)
            .FirstOrDefaultAsync();

        if (consultation == null)
            return NotFound(new { message = "Consultation non trouvée" });

        return Ok(consultation);
    }

    [HttpPost]
    public async Task<IActionResult> CreateConsultation([FromBody] CreateConsultationRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Données invalides" });
        
        var doctorId = GetCurrentUserId();
        
        var consultation = new Consultation
        {
            PatientName = request.PatientName ?? "",
            PatientEmail = request.PatientEmail ?? "",
            Symptoms = request.Symptoms ?? "",
            Diagnosis = request.Diagnosis ?? "",
            Recommendations = request.Recommendations ?? "",
            ConfidenceScore = request.ConfidenceScore,
            Status = "Terminée",
            CompletedAt = DateTime.UtcNow,
            DoctorId = doctorId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Consultations.Add(consultation);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Consultation enregistrée", consultationId = consultation.Id });
    }
}

public class CreateConsultationRequest
{
    public string PatientName { get; set; } = "";
    public string PatientEmail { get; set; } = "";
    public string Symptoms { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public string Recommendations { get; set; } = "";
    public double ConfidenceScore { get; set; }
}