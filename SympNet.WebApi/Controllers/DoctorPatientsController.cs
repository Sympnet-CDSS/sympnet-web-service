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

    [HttpGet]
    public async Task<IActionResult> GetMyPatients()
    {
        var doctorId = GetCurrentDoctorId();
        
        try 
        {
            // Récupérer tous les patients
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
                    PhotoUrl = p.User.PhotoUrl,
                    p.CreatedAt
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
     
            return Ok(patients);
        }
        catch (Exception)
        {
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
                    PhotoUrl = p.User.PhotoUrl,
                    p.CreatedAt
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Ok(patients);
        }
    }

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
                    PhotoUrl = p.User.PhotoUrl,
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
                    PhotoUrl = p.User.PhotoUrl,
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