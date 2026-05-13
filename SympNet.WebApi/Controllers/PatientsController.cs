using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Dtos;
using SympNet.WebApi.Models;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/patients
    [HttpGet]
    public async Task<IActionResult> GetAllPatients()
    {
        try 
        {
            var patients = await _db.Patients
                .Include(p => p.User)
                .Select(p => new PatientDto
                {
                    Id = p.Id,
                    Email = p.User.Email,
                    IsActive = p.User.IsActive,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PhoneNumber = p.PhoneNumber,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    ConsultationCount = p.ConsultationCount,
                    MedicalHistory = p.MedicalHistory,
                    ChronicConditions = string.IsNullOrEmpty(p.ChronicDiseases) ? new List<string>() : p.ChronicDiseases.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    CurrentMedications = string.IsNullOrEmpty(p.CurrentMedications) ? new List<string>() : p.CurrentMedications.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Allergies = string.IsNullOrEmpty(p.Allergies) ? new List<string>() : p.Allergies.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                })
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return Ok(patients);
        }
        catch (Exception)
        {
            var patients = await _db.Patients
                .Include(p => p.User)
                .Select(p => new PatientDto
                {
                    Id = p.Id,
                    Email = p.User.Email,
                    IsActive = p.User.IsActive,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PhoneNumber = p.PhoneNumber,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    ConsultationCount = p.ConsultationCount,
                    MedicalHistory = p.MedicalHistory,
                    Allergies = string.IsNullOrEmpty(p.Allergies) ? new List<string>() : p.Allergies.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                })
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            return Ok(patients);
        }
    }

    // GET: api/patients/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatient(int id)
    {
        try 
        {
            var patient = await _db.Patients
                .Include(p => p.User)
                .Where(p => p.Id == id)
                .Select(p => new PatientDto
                {
                    Id = p.Id,
                    Email = p.User.Email,
                    IsActive = p.User.IsActive,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PhoneNumber = p.PhoneNumber,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    ConsultationCount = p.ConsultationCount,
                    MedicalHistory = p.MedicalHistory,
                    ChronicConditions = string.IsNullOrEmpty(p.ChronicDiseases) ? new List<string>() : p.ChronicDiseases.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    CurrentMedications = string.IsNullOrEmpty(p.CurrentMedications) ? new List<string>() : p.CurrentMedications.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Allergies = string.IsNullOrEmpty(p.Allergies) ? new List<string>() : p.Allergies.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
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
                .Select(p => new PatientDto
                {
                    Id = p.Id,
                    Email = p.User.Email,
                    IsActive = p.User.IsActive,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PhoneNumber = p.PhoneNumber,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    ConsultationCount = p.ConsultationCount,
                    MedicalHistory = p.MedicalHistory,
                    Allergies = string.IsNullOrEmpty(p.Allergies) ? new List<string>() : p.Allergies.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                })
                .FirstOrDefaultAsync();
            return Ok(patient);
        }
    }

    // POST: api/patients
    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto)
    {
        // Vérifier si l'email existe déjà
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Cet email est déjà utilisé" });

        // Créer l'utilisateur
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Patient",
            IsActive = true,
            FullName = $"{dto.FirstName} {dto.LastName}".Trim()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Créer le patient
        var patient = new Patient
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber ?? string.Empty,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            ConsultationCount = 0
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Patient créé avec succès", patientId = patient.Id });
    }

    // PUT: api/patients/{id}/activate
    [HttpPut("{id}/activate")]
    public async Task<IActionResult> ActivatePatient(int id)
    {
        var patient = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null)
            return NotFound(new { message = "Patient non trouvé" });

        patient.User.IsActive = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Patient activé" });
    }

    // PUT: api/patients/{id}/deactivate
    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivatePatient(int id)
    {
        var patient = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null)
            return NotFound(new { message = "Patient non trouvé" });

        patient.User.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Patient désactivé" });
    }
    
    [HttpPut("{id}/medical-background")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> UpdateMedicalBackground(int id, [FromBody] MedicalBackgroundUpdateDto dto)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null)
            return NotFound(new { message = "Patient non trouvé" });

        patient.MedicalHistory = dto.MedicalHistory ?? patient.MedicalHistory;
        patient.Allergies = dto.Allergies ?? patient.Allergies;
        patient.ChronicDiseases = dto.ChronicDiseases ?? patient.ChronicDiseases;
        patient.CurrentMedications = dto.CurrentMedications ?? patient.CurrentMedications;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Dossier médical mis à jour" });
    }
}

public class MedicalBackgroundUpdateDto
{
    public string? MedicalHistory { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? CurrentMedications { get; set; }
}