using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Dtos;
using SympNet.WebApi.Models;
using SympNet.WebApi.Services;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;

    public DoctorsController(AppDbContext db, EmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    // GET: api/doctors
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAllDoctors()
    {
        var doctors = await _db.Doctors
            .Include(d => d.User)
            .Select(d => new DoctorDto
            {
                Id = d.Id,
                Email = d.User.Email,
                IsActive = d.User.IsActive,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Speciality = d.Speciality,
                LicenseNumber = d.LicenseNumber,
                Address = d.Address,
                Latitude = d.Latitude ?? 0,
                Longitude = d.Longitude ?? 0,
                CreatedAt = d.CreatedAt
            })
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Ok(doctors);
    }

    // POST: api/doctors
    [HttpPost]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Cet email est déjà utilisé" });

        var tempPassword = GenerateRandomPassword();
        
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            Role = "Doctor",
            IsActive = true,
            FullName = $"{dto.FirstName} {dto.LastName}".Trim()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var doctor = new Doctor
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Speciality = dto.Speciality,
            LicenseNumber = dto.LicenseNumber,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            CreatedAt = DateTime.UtcNow
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();

        try
        {
            await _emailService.SendDoctorCredentialsAsync(dto.Email, dto.FirstName, tempPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur envoi email: {ex.Message}");
        }

        return Ok(new 
        { 
            message = "Médecin créé avec succès. Un email avec ses identifiants lui a été envoyé.",
            doctorId = doctor.Id,
            tempPassword
        });
    }

    // PUT: api/doctors/{id}/activate
    [HttpPut("{id}/activate")]
    public async Task<IActionResult> ActivateDoctor(int id)
    {
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null)
            return NotFound(new { message = "Médecin non trouvé" });

        doctor.User.IsActive = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Médecin activé" });
    }

    // PUT: api/doctors/{id}/deactivate
    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivateDoctor(int id)
    {
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null)
            return NotFound(new { message = "Médecin non trouvé" });

        doctor.User.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Médecin désactivé" });
    }

    // DELETE: api/doctors/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null)
            return NotFound(new { message = "Médecin non trouvé" });

        _db.Doctors.Remove(doctor);
        _db.Users.Remove(doctor.User);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Médecin supprimé avec succès" });
    }

    private string GenerateRandomPassword(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}