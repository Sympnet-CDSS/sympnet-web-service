using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Dtos;
using SympNet.WebApi.Models;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/patients
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
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
    public async Task<IActionResult> GetPatient(string id)
    {
        Patient patient = null;
        if (Guid.TryParse(id, out Guid userGuid))
        {
            patient = await _db.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userGuid);

            // Defensive Fallback: Auto-create if user exists but patient does not
            if (patient == null)
            {
                var user = await _db.Users.FindAsync(userGuid);
                if (user != null && user.Role == "Patient")
                {
                    patient = new Patient
                    {
                        UserId = user.Id,
                        FirstName = user.FullName?.Split(' ')[0] ?? "Patient",
                        LastName = user.FullName?.Contains(' ') == true ? user.FullName.Substring(user.FullName.IndexOf(' ') + 1) : "",
                        PhoneNumber = "",
                        DateOfBirth = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc),
                        Gender = "Non renseigné",
                        BloodType = "Non renseigné",
                        Allergies = "",
                        MedicalHistory = "",
                        ChronicDiseases = "",
                        CurrentMedications = "",
                        ConsultationCount = 0
                    };
                    _db.Patients.Add(patient);
                    await _db.SaveChangesAsync();
                }
            }
        }
        else if (int.TryParse(id, out int patientId))
        {
            patient = await _db.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);
        }

        if (patient == null)
            return NotFound(new { message = "Patient non trouvé" });

        return Ok(new PatientDto
        {
            Id = patient.Id,
            Email = patient.User.Email,
            IsActive = patient.User.IsActive,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            PhoneNumber = patient.PhoneNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Address = patient.Address,
            BloodType = patient.BloodType,
            ConsultationCount = patient.ConsultationCount,
            MedicalHistory = patient.MedicalHistory,
            PhotoUrl = patient.User.PhotoUrl,
            ChronicConditions = string.IsNullOrEmpty(patient.ChronicDiseases) ? new List<string>() : patient.ChronicDiseases.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            CurrentMedications = string.IsNullOrEmpty(patient.CurrentMedications) ? new List<string>() : patient.CurrentMedications.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Allergies = string.IsNullOrEmpty(patient.Allergies) ? new List<string>() : patient.Allergies.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        });
    }

    // PUT: api/patients/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePatient(string id, [FromBody] UpdatePatientDto dto)
    {
        Patient patient = null;
        if (Guid.TryParse(id, out Guid userGuid))
        {
            patient = await _db.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userGuid);
        }
        else if (int.TryParse(id, out int patientId))
        {
            patient = await _db.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);
        }

        if (patient == null)
            return NotFound(new { message = "Patient non trouvé" });

        if (!string.IsNullOrEmpty(dto.FirstName))
            patient.FirstName = dto.FirstName;

        if (!string.IsNullOrEmpty(dto.LastName))
            patient.LastName = dto.LastName;

        if (dto.DateOfBirth.HasValue)
        {
            var dob = dto.DateOfBirth.Value;
            if (dob.Kind == DateTimeKind.Unspecified)
            {
                patient.DateOfBirth = DateTime.SpecifyKind(dob, DateTimeKind.Utc);
            }
            else
            {
                patient.DateOfBirth = dob.ToUniversalTime();
            }
        }

        if (!string.IsNullOrEmpty(dto.Gender))
            patient.Gender = dto.Gender;

        if (!string.IsNullOrEmpty(dto.PhoneNumber))
            patient.PhoneNumber = dto.PhoneNumber;

        if (!string.IsNullOrEmpty(dto.Address))
            patient.Address = dto.Address;

        if (!string.IsNullOrEmpty(dto.BloodType))
            patient.BloodType = dto.BloodType;

        if (!string.IsNullOrEmpty(dto.MedicalHistory))
            patient.MedicalHistory = dto.MedicalHistory;

        if (dto.Allergies != null)
        {
            patient.Allergies = string.Join(",", dto.Allergies);
        }

        if (patient.User != null)
        {
            if (!string.IsNullOrEmpty(dto.PhotoUrl))
            {
                patient.User.PhotoUrl = dto.PhotoUrl;
            }
            patient.User.FullName = $"{patient.FirstName} {patient.LastName}".Trim();
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Profil patient mis à jour avec succès" });
    }

    // POST: api/patients
    [HttpPost]
    [Authorize(Roles = "Admin")]
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
            DateOfBirth = dto.DateOfBirth.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc) : dto.DateOfBirth.ToUniversalTime(),
            Gender = dto.Gender,
            ConsultationCount = 0
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Patient créé avec succès", patientId = patient.Id });
    }

    // PUT: api/patients/{id}/activate
    [HttpPut("{id}/activate")]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivatePatient(int id)
    {
        var patient = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null)
            return NotFound(new { message = "Patient non trouvé" });

        patient.User.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Patient désactivé" });
    }
}