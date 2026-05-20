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

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return NotFound(new { message = "Médecin non trouvé" });

        return Ok(new
        {
            doctor.Id,
            doctor.User.Email,
            doctor.FirstName,
            doctor.LastName,
            doctor.Speciality,
            doctor.PhoneNumber,
            doctor.LicenseNumber,
            doctor.Bio,
            doctor.GraduationYear,
            doctor.Address,
            doctor.Latitude,
            doctor.Longitude,
            doctor.TotalConsultations,
            doctor.TotalPatients,
            PhotoUrl = doctor.User.PhotoUrl,
            IsActive = doctor.User.IsActive
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = GetCurrentUserId();
        
        var totalPatients = await _db.Consultations
            .Where(c => c.DoctorId == userId)
            .Select(c => c.PatientEmail)
            .Distinct()
            .CountAsync();
        
        var consultations = await _db.Consultations
            .Where(c => c.DoctorId == userId)
            .ToListAsync();
        
        var todayConsultations = consultations.Count(c => c.CreatedAt.Date == DateTime.UtcNow.Date);
        var totalConsultations = consultations.Count;
        
        return Ok(new { totalPatients, todayConsultations, totalConsultations });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateDoctorProfileDto dto)
    {
        var userId = GetCurrentUserId();
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        
        if (doctor == null)
            return NotFound(new { message = "Médecin non trouvé" });
        
        if (!string.IsNullOrEmpty(dto.FirstName)) doctor.FirstName = dto.FirstName;
        if (!string.IsNullOrEmpty(dto.LastName)) doctor.LastName = dto.LastName;
        if (!string.IsNullOrEmpty(dto.Speciality)) doctor.Speciality = dto.Speciality;
        if (dto.PhoneNumber != null) doctor.PhoneNumber = dto.PhoneNumber;
        if (!string.IsNullOrEmpty(dto.LicenseNumber)) doctor.LicenseNumber = dto.LicenseNumber;
        if (dto.Bio != null) doctor.Bio = dto.Bio;
        if (dto.GraduationYear.HasValue) doctor.GraduationYear = dto.GraduationYear.Value;
        if (!string.IsNullOrEmpty(dto.Address)) doctor.Address = dto.Address;
        
        await _db.SaveChangesAsync();
        return Ok(new { message = "Profil mis à jour avec succès" });
    }

    [HttpPut("password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdateDoctorPasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);
        
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });
        
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mot de passe actuel incorrect" });
        
        if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest(new { message = "Le nouveau mot de passe doit contenir au moins 6 caractères" });
        
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();
        
        return Ok(new { message = "Mot de passe mis à jour avec succès" });
    }
}