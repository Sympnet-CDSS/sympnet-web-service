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

    private Guid GetCurrentDoctorId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException();
        
        var doctor = _db.Doctors.FirstOrDefault(d => d.UserId == Guid.Parse(userIdClaim));
        return doctor?.UserId ?? Guid.Parse(userIdClaim);
    }

    [HttpGet]
    public async Task<IActionResult> GetConsultations()
    {
        var doctorId = GetCurrentDoctorId();
        var consultations = await _db.Consultations
            .Where(c => c.DoctorId == doctorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(consultations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetConsultation(int id)
    {
        var doctorId = GetCurrentDoctorId();
        var consultation = await _db.Consultations
            .FirstOrDefaultAsync(c => c.Id == id && c.DoctorId == doctorId);

        if (consultation == null)
            return NotFound(new { message = "Consultation non trouvée" });

        return Ok(consultation);
    }

    [HttpPost]
    public async Task<IActionResult> CreateConsultation([FromBody] CreateConsultationDto dto)
    {
        var doctorId = GetCurrentDoctorId();
        
        var consultation = new Consultation
        {
            PatientName = dto.PatientName,
            PatientEmail = dto.PatientEmail,
            Symptoms = dto.Symptoms,
            Diagnosis = dto.Diagnosis,
            Recommendations = dto.Recommendations,
            ConfidenceScore = dto.ConfidenceScore,
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

public class CreateConsultationDto
{
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}