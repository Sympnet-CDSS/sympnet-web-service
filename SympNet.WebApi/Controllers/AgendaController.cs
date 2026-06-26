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
public class AgendaController : ControllerBase
{
    private readonly AppDbContext _db;

    public AgendaController(AppDbContext db)
    {
        _db = db;
    }

    private int GetCurrentDoctorId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException();
        var userId = Guid.Parse(userIdClaim);
        var doctor = _db.Doctors.FirstOrDefault(d => d.UserId == userId);
        if (doctor == null) throw new UnauthorizedAccessException("Doctor profile not found.");
        return doctor.Id;
    }

    private static DateTime ToUtc(DateTime dt)
    {
        return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var doctorId = GetCurrentDoctorId();

        var startUtc = ToUtc(start);
        var endUtc = ToUtc(end);

        var events = new List<AgendaEventDto>();

        try
        {
            var appointments = await _db.Appointments
                .Where(a => a.DateTime >= startUtc && a.DateTime <= endUtc && a.Status != "Annulé")
                .ToListAsync();

            var doctorAppointments = appointments
                .Where(a => a.DoctorId == doctorId)
                .ToList();

            foreach (var a in doctorAppointments)
            {
                events.Add(new AgendaEventDto
                {
                    Id = a.Id,
                    Title = "Rendez-vous",
                    Start = a.DateTime,
                    End = a.DateTime.AddMinutes(30),
                    Type = "appointment",
                    Status = a.Status,
                    Color = a.Status == "Urgent" ? "#EF4444" : "#0D9488",
                    PatientName = $"Patient {a.PatientId.ToString().Substring(0, 8)}",
                    IsUrgent = a.Status == "Urgent"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur Appointments: {ex.Message}");
        }

        try
        {
            var blockedSlots = await _db.BlockedSlots
                .Where(b => b.DoctorId == doctorId && b.StartDateTime >= startUtc && b.StartDateTime <= endUtc)
                .ToListAsync();

            foreach (var b in blockedSlots)
            {
                events.Add(new AgendaEventDto
                {
                    Id = b.Id,
                    Title = $"Bloqué - {b.Reason}",
                    Start = b.StartDateTime,
                    End = b.EndDateTime,
                    Type = "blocked",
                    Status = "Bloqué",
                    Color = "#EF4444",
                    Reason = b.Reason
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur BlockedSlots: {ex.Message}");
        }

        return Ok(events);
    }

    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] DateTime date)
    {
        var doctorId = GetCurrentDoctorId();
        var availableSlots = new List<AvailableSlotResponseDto>();

        try
        {
            var dateUtc = ToUtc(date);

            var dayOfWeek = (int)dateUtc.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var workingHours = await _db.WorkingHours
                .FirstOrDefaultAsync(w => w.DoctorId == doctorId && (int)w.DayOfWeek == dayOfWeek && w.IsActive);

            if (workingHours == null)
                return Ok(availableSlots);

            var startTime = workingHours.StartTime;
            var endTime = workingHours.EndTime;
            var slotDuration = workingHours.SlotDuration;
            var currentSlot = DateTime.SpecifyKind(dateUtc.Date + startTime.ToTimeSpan(), DateTimeKind.Utc);
            var endDateTime = DateTime.SpecifyKind(dateUtc.Date + endTime.ToTimeSpan(), DateTimeKind.Utc);

            var allAppointments = await _db.Appointments
                .Where(a => a.DateTime >= currentSlot && a.DateTime < endDateTime && a.Status != "Annulé")
                .ToListAsync();

            var existingAppointments = allAppointments
                .Where(a => a.DoctorId == doctorId)
                .Select(a => a.DateTime)
                .ToList();

            var blockedSlots = await _db.BlockedSlots
                .Where(b => b.DoctorId == doctorId && b.StartDateTime >= currentSlot && b.StartDateTime < endDateTime)
                .ToListAsync();

            while (currentSlot.AddMinutes(slotDuration) <= endDateTime)
            {
                var slotEnd = currentSlot.AddMinutes(slotDuration);
                var isBooked = existingAppointments.Any(a => a >= currentSlot && a < slotEnd);
                var isBlocked = blockedSlots.Any(b => b.StartDateTime <= currentSlot && b.EndDateTime >= slotEnd);

                availableSlots.Add(new AvailableSlotResponseDto
                {
                    StartTime = currentSlot,
                    EndTime = slotEnd,
                    IsAvailable = !isBooked && !isBlocked
                });

                currentSlot = slotEnd;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur available-slots: {ex.Message}");
        }

        return Ok(availableSlots);
    }
}

public static class TimeOnlyExtensions
{
    public static TimeSpan ToTimeSpan(this TimeOnly time)
    {
        return new TimeSpan(time.Hour, time.Minute, 0);
    }
}