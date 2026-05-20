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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConsultation(int id)
    {
        var doctorId = GetCurrentUserId();
        var consultation = await _db.Consultations
            .Where(c => c.Id == id && c.DoctorId == doctorId)
            .FirstOrDefaultAsync();

        if (consultation == null)
            return NotFound(new { message = "Consultation non trouvée" });

        _db.Consultations.Remove(consultation);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Consultation supprimée avec succès" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConsultation(int id, [FromBody] UpdateConsultationRequest request)
    {
        var doctorId = GetCurrentUserId();
        var consultation = await _db.Consultations
            .Where(c => c.Id == id && c.DoctorId == doctorId)
            .FirstOrDefaultAsync();

        if (consultation == null)
            return NotFound(new { message = "Consultation non trouvée" });

        consultation.Diagnosis = request.Diagnosis ?? consultation.Diagnosis;
        consultation.Recommendations = request.Recommendations ?? consultation.Recommendations;
        consultation.Symptoms = request.Symptoms ?? consultation.Symptoms;
        
        await _db.SaveChangesAsync();

        return Ok(new { message = "Consultation mise à jour avec succès" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateConsultation([FromBody] CreateConsultationRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Données invalides" });
        
        if (string.IsNullOrEmpty(request.PatientEmail))
            return BadRequest(new { message = "L'email du patient est requis" });
        
        var doctorId = GetCurrentUserId();
        
        // Vérifier si le patient existe, sinon le créer
        var patient = await _db.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.User.Email == request.PatientEmail);
        
        if (patient == null)
        {
            // Créer l'utilisateur patient
            var user = new User
            {
                Email = request.PatientEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient@123"), // Mot de passe par défaut
                Role = "Patient",
                IsActive = true,
                FullName = request.PatientName,
                CreatedAt = DateTime.UtcNow
            };
            
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            
            // Créer le patient
            patient = new Patient
            {
                UserId = user.Id,
                FirstName = request.PatientName?.Split(' ').FirstOrDefault() ?? "",
                LastName = request.PatientName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                PhoneNumber = "",
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                Gender = "Non spécifié",
                ConsultationCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();
            
            Console.WriteLine($"Patient créé: {request.PatientEmail}");
        }
        else
        {
            // Incrémenter le compteur de consultations
            patient.ConsultationCount++;
            await _db.SaveChangesAsync();
        }
        
        // Créer la consultation
        var consultation = new Consultation
        {
            PatientName = request.PatientName ?? patient.FirstName + " " + patient.LastName,
            PatientEmail = request.PatientEmail,
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

        return Ok(new { 
            message = "Consultation enregistrée avec succès", 
            consultationId = consultation.Id,
            patientId = patient.Id,
            patientCreated = patient != null 
        });
    }
}

public class UpdateConsultationRequest
{
    public string? Diagnosis { get; set; }
    public string? Recommendations { get; set; }
    public string? Symptoms { get; set; }
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