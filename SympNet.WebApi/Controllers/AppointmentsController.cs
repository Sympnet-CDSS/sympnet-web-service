// Controllers/AppointmentsController.cs - Version simplifiée
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Dtos;
using SympNet.WebApi.Models;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AppointmentsController(AppDbContext db)
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
    [Authorize(Roles = "Patient,Doctor")]
    public async Task<IActionResult> GetMyAppointments()
    {
        var userId = GetCurrentUserId();
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Doctor")
        {
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return Unauthorized();

            var appointments = await _db.Appointments
                .Where(a => a.DoctorId == doctor.Id)
                .OrderBy(a => a.DateTime)
                .ToListAsync();
            return Ok(appointments);
        }
        else
        {
            var appointments = await _db.Appointments
                .Where(a => a.PatientId == userId)
                .OrderBy(a => a.DateTime)
                .ToListAsync();
            return Ok(appointments);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Patient,Doctor")]
    public async Task<IActionResult> CreateAppointment([FromBody] System.Text.Json.JsonElement dto)
    {
        var userId = GetCurrentUserId();
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Doctor")
        {
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return Unauthorized();

            var email = dto.GetProperty("patientEmail").GetString();
            var patientUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Role == "Patient");
            
            if (patientUser == null) 
                return BadRequest(new { message = "Patient introuvable avec cet email." });

            var appointment = new Appointment
            {
                PatientId = patientUser.Id,
                DoctorId = doctor.Id,
                DateTime = dto.GetProperty("dateTime").GetDateTime().ToUniversalTime(),
                Duration = dto.TryGetProperty("duration", out var dur) ? dur.GetInt32() : 30,
                Type = dto.TryGetProperty("type", out var type) ? type.GetString() : "Consultation",
                Notes = dto.TryGetProperty("notes", out var notes) ? notes.GetString() : "",
                Status = "Confirmé", // A doctor creating it auto-confirms it
                CreatedAt = DateTime.UtcNow
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Rendez-vous créé avec succès", appointmentId = appointment.Id });
        }
        else
        {
            // Patient logic
            var appointment = new Appointment
            {
                PatientId = userId,
                DoctorId = dto.GetProperty("doctorId").GetInt32(),
                DateTime = dto.GetProperty("dateTime").GetDateTime().ToUniversalTime(),
                Status = "En attente",
                Notes = dto.TryGetProperty("notes", out var notes) ? notes.GetString() : "",
                CreatedAt = DateTime.UtcNow
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Rendez-vous créé avec succès", appointmentId = appointment.Id });
        }
    }
}