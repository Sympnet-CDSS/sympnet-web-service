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

    // GET: api/appointments (Patient)
    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyAppointments()
    {
        var userId = GetCurrentUserId();
        
        var appointments = await _db.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == userId)
            .OrderBy(a => a.DateTime)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor != null ? $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}" : "Docteur",
                DoctorSpeciality = a.Doctor != null ? a.Doctor.Speciality : "Généraliste",
                DoctorAddress = a.Doctor != null ? a.Doctor.Address : "Adresse non renseignée",
                DateTime = a.DateTime,
                Status = a.Status,
                Notes = a.Notes
            })
            .ToListAsync();

        return Ok(appointments);
    }

    // GET: api/appointments/doctor/{doctorId}
    [HttpGet("doctor/{doctorId}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetDoctorAppointments(int doctorId)
    {
        var userId = GetCurrentUserId();
        
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        if (doctor == null || doctor.Id != doctorId)
            return Unauthorized();

        var appointments = await _db.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctorId)
            .OrderBy(a => a.DateTime)
            .Select(a => new
            {
                a.Id,
                PatientName = a.Patient != null ? a.Patient.FullName : "Patient",
                PatientEmail = a.Patient != null ? a.Patient.Email : "Email non renseigné",
                a.DateTime,
                a.Status,
                a.Notes
            })
            .ToListAsync();

        return Ok(appointments);
    }

    // POST: api/appointments
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        var userId = GetCurrentUserId();
        
        var doctor = await _db.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null)
            return NotFound(new { message = "Médecin non trouvé" });

        var existingAppointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.DoctorId == dto.DoctorId && a.DateTime == dto.DateTime);
        
        if (existingAppointment != null)
            return BadRequest(new { message = "Ce créneau est déjà pris" });

        var appointment = new Appointment
        {
            PatientId = userId,
            DoctorId = dto.DoctorId,
            DateTime = dto.DateTime,
            Status = "En attente",
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Rendez-vous créé avec succès", appointmentId = appointment.Id });
    }

    // PUT: api/appointments/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto dto)
    {
        var userId = GetCurrentUserId();
        
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound(new { message = "Rendez-vous non trouvé" });

        var isPatient = appointment.PatientId == userId;
        var isDoctor = await _db.Doctors.AnyAsync(d => d.UserId == userId && d.Id == appointment.DoctorId);

        if (!isPatient && !isDoctor)
            return Unauthorized();

        if (dto.DateTime.HasValue)
        {
            var existingAppointment = await _db.Appointments
                .FirstOrDefaultAsync(a => a.DoctorId == appointment.DoctorId && 
                                         a.DateTime == dto.DateTime.Value &&
                                         a.Id != id);
            
            if (existingAppointment != null)
                return BadRequest(new { message = "Ce créneau est déjà pris" });
                
            appointment.DateTime = dto.DateTime.Value;
            appointment.Status = "En attente";
        }

        if (!string.IsNullOrEmpty(dto.Status))
            appointment.Status = dto.Status;

        if (!string.IsNullOrEmpty(dto.Notes))
            appointment.Notes = dto.Notes;

        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Rendez-vous mis à jour" });
    }

    // DELETE: api/appointments/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var userId = GetCurrentUserId();
        
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound(new { message = "Rendez-vous non trouvé" });

        var isPatient = appointment.PatientId == userId;
        var isDoctor = await _db.Doctors.AnyAsync(d => d.UserId == userId && d.Id == appointment.DoctorId);

        if (!isPatient && !isDoctor)
            return Unauthorized();

        appointment.Status = "Annulé";
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Rendez-vous annulé" });
    }
}