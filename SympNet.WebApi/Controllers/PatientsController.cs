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
                ConsultationCount = p.ConsultationCount
            })
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return Ok(patients);
    }

    // GET: api/patients/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatient(int id)
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
                ConsultationCount = p.ConsultationCount
            })
            .FirstOrDefaultAsync();

        if (patient == null)
            return NotFound(new { message = "Patient non trouvé" });

        return Ok(patient);
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

    // DELETE: api/patients/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var patient = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null)
            return NotFound(new { message = "Patient non trouvé" });

        var user = patient.User;
        _db.Patients.Remove(patient);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Patient supprimé avec succès" });
    }
}