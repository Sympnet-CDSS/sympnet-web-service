using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Dtos;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor")]
public class DoctorProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorProfileController(AppDbContext db)
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

    // GET: api/doctorprofile
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return NotFound(new { message = "Médecin non trouvé" });

        return Ok(new DoctorProfileResponseDto
        {
            Id = doctor.Id,
            Email = doctor.User.Email,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            Speciality = doctor.Speciality,
            LicenseNumber = doctor.LicenseNumber,
            Address = doctor.Address,
            Latitude = doctor.Latitude,
            Longitude = doctor.Longitude,
            IsActive = doctor.User.IsActive
        });
    }

    // PUT: api/doctorprofile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateDoctorProfileDto dto)
    {
        var userId = GetCurrentUserId();
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return NotFound(new { message = "Médecin non trouvé" });

        // Mettre à jour les champs
        if (!string.IsNullOrEmpty(dto.FirstName))
            doctor.FirstName = dto.FirstName;
        if (!string.IsNullOrEmpty(dto.LastName))
            doctor.LastName = dto.LastName;
        if (!string.IsNullOrEmpty(dto.Speciality))
            doctor.Speciality = dto.Speciality;
        if (!string.IsNullOrEmpty(dto.LicenseNumber))
            doctor.LicenseNumber = dto.LicenseNumber;
        if (!string.IsNullOrEmpty(dto.Address))
            doctor.Address = dto.Address;
        if (dto.Latitude.HasValue)
            doctor.Latitude = dto.Latitude;
        if (dto.Longitude.HasValue)
            doctor.Longitude = dto.Longitude;

        // Mettre à jour l'email si changé
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != doctor.User.Email)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId))
                return BadRequest(new { message = "Cet email est déjà utilisé" });
            doctor.User.Email = dto.Email;
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Profil mis à jour avec succès" });
    }

    // PUT: api/doctorprofile/password
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeDoctorPasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mot de passe actuel incorrect" });

        if (dto.NewPassword.Length < 6)
            return BadRequest(new { message = "Le nouveau mot de passe doit contenir au moins 6 caractères" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Mot de passe mis à jour avec succès" });
    }

    // GET: api/doctorprofile/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = GetCurrentUserId();
        
        var totalPatients = await _db.Patients.CountAsync();
        var todayConsultations = 0; // À implémenter avec une table Consultations
        var totalConsultations = 0; // À implémenter avec une table Consultations

        return Ok(new DoctorStatsDto
        {
            TotalPatients = totalPatients,
            TodayConsultations = todayConsultations,
            TotalConsultations = totalConsultations
        });
    }
}

// DTOs pour le médecin
public class DoctorProfileResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Speciality { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateDoctorProfileDto
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Speciality { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class ChangeDoctorPasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class DoctorStatsDto
{
    public int TotalPatients { get; set; }
    public int TodayConsultations { get; set; }
    public int TotalConsultations { get; set; }
}