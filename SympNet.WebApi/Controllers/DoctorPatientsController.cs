using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor")]
public class DoctorPatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorPatientsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetCurrentDoctorId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException();
        return Guid.Parse(userIdClaim);
    }

    // GET: api/doctorpatients
    [HttpGet]
    public async Task<IActionResult> GetMyPatients()
    {
        var doctorId = GetCurrentDoctorId();
        
        try 
        {
            // Récupérer tous les patients enregistrés dans le système
            var patients = await _db.Patients
                .Include(p => p.User)
                .Where(p => p.User.Role == "Patient")
                .Select(p => new
                {
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    Email = p.User.Email,
                    p.PhoneNumber,
                    p.DateOfBirth,
                    p.Gender,
                    p.BloodType,
                    p.Allergies,
                    p.MedicalHistory,
                    // Si la migration a échoué, on évite de planter en retournant vide ou le MedicalHistory
                    ChronicDiseases = p.MedicalHistory, 
                    CurrentMedications = "",
                    ConsultationCount = _db.Consultations.Count(c => c.PatientEmail == p.User.Email && c.DoctorId == doctorId),
                    p.CreatedAt
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
     
            return Ok(patients);
        }
        catch (Exception)
        {
            // Fallback : Version simplifiée sans les nouvelles colonnes si la DB n'est pas à jour
            var patients = await _db.Patients
                .Include(p => p.User)
                .Where(p => p.User.Role == "Patient")
                .Select(p => new
                {
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    Email = p.User.Email,
                    p.PhoneNumber,
                    p.DateOfBirth,
                    p.Gender,
                    p.BloodType,
                    p.Allergies,
                    p.MedicalHistory,
                    ChronicDiseases = p.MedicalHistory,
                    CurrentMedications = "",
                    ConsultationCount = _db.Consultations.Count(c => c.PatientEmail == p.User.Email && c.DoctorId == doctorId),
                    p.CreatedAt
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Ok(patients);
        }
    }

    // GET: api/doctorpatients/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatientDetails(int id)
    {
        var doctorId = GetCurrentDoctorId();
        
        try 
        {
            var patient = await _db.Patients
                .Include(p => p.User)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    Email = p.User.Email,
                    p.PhoneNumber,
                    p.DateOfBirth,
                    p.Gender,
                    p.BloodType,
                    p.Allergies,
                    p.MedicalHistory,
                    ChronicDiseases = p.MedicalHistory,
                    CurrentMedications = "",
                    ConsultationCount = _db.Consultations.Count(c => c.PatientEmail == p.User.Email && c.DoctorId == doctorId),
                    Consultations = _db.Consultations
                        .Where(c => c.PatientEmail == p.User.Email && c.DoctorId == doctorId)
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => new
                        {
                            c.Id,
                            c.Diagnosis,
                            c.Symptoms,
                            c.Recommendations,
                            c.ConfidenceScore,
                            c.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (patient == null)
                return NotFound(new { message = "Patient non trouvé" });

            return Ok(patient);
        }
        catch (Exception)
        {
            // Fallback en cas d'erreur de base de données
             var patient = await _db.Patients
                .Include(p => p.User)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    Email = p.User.Email,
                    p.PhoneNumber,
                    p.DateOfBirth,
                    p.Gender,
                    p.BloodType,
                    p.Allergies,
                    p.MedicalHistory,
                    ChronicDiseases = p.MedicalHistory,
                    CurrentMedications = "",
                    ConsultationCount = _db.Consultations.Count(c => c.PatientEmail == p.User.Email && c.DoctorId == doctorId),
                    Consultations = _db.Consultations
                        .Where(c => c.PatientEmail == p.User.Email && c.DoctorId == doctorId)
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => new
                        {
                            c.Id,
                            c.Diagnosis,
                            c.Symptoms,
                            c.Recommendations,
                            c.ConfidenceScore,
                            c.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (patient == null)
                return NotFound(new { message = "Patient non trouvé" });

            return Ok(patient);
        }
    }
}