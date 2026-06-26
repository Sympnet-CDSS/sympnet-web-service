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
[Authorize(Roles = "Doctor")]
public class WorkingHoursController : ControllerBase
{
    private readonly AppDbContext _db;

    public WorkingHoursController(AppDbContext db)
    {
        _db = db;
    }

    private int GetCurrentDoctorId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException();

        var doctor = _db.Doctors.FirstOrDefault(d => d.UserId == Guid.Parse(userIdClaim));
        if (doctor == null) throw new UnauthorizedAccessException("Doctor profile not found.");
        return doctor.Id;
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkingHours()
    {
        var doctorId = GetCurrentDoctorId();

        var raw = await _db.Set<WorkingHours>()
            .Where(w => w.DoctorId == doctorId)
            .OrderBy(w => w.DayOfWeek)
            .ToListAsync();

        if (!raw.Any())
        {
            var defaultHours = Enumerable.Range(1, 5).Select(i => new
            {
                Id = 0,
                DayOfWeek = i,
                DayName = GetDayName(i),
                StartTime = "09:00",
                EndTime = "17:00",
                SlotDuration = 30,
                IsActive = true
            }).ToList<object>();

            return Ok(defaultHours);
        }

        var workingHours = raw.Select(w => new
        {
            w.Id,
            DayOfWeek = (int)w.DayOfWeek,
            DayName = GetDayName((int)w.DayOfWeek),
            StartTime = w.StartTime.ToString(@"HH\:mm"),
            EndTime = w.EndTime.ToString(@"HH\:mm"),
            w.SlotDuration,
            w.IsActive
        }).ToList();

        return Ok(workingHours);
    }

    [HttpGet("doctor/{doctorId}")]
    [AllowAnonymous] 
    public async Task<IActionResult> GetDoctorWorkingHours(int doctorId)
    {
        var raw = await _db.Set<WorkingHours>()
            .Where(w => w.DoctorId == doctorId)
            .OrderBy(w => w.DayOfWeek)
            .ToListAsync();

        if (!raw.Any())
        {
            var defaultHours = Enumerable.Range(1, 5).Select(i => new
            {
                Id = 0,
                DayOfWeek = i,
                DayName = GetDayName(i),
                StartTime = "09:00",
                EndTime = "17:00",
                SlotDuration = 30,
                IsActive = true
            }).ToList<object>();

            return Ok(defaultHours);
        }

        var workingHours = raw.Select(w => new
        {
            w.Id,
            DayOfWeek = (int)w.DayOfWeek,
            DayName = GetDayName((int)w.DayOfWeek),
            StartTime = w.StartTime.ToString(@"HH\:mm"),
            EndTime = w.EndTime.ToString(@"HH\:mm"),
            w.SlotDuration,
            w.IsActive
        }).ToList();

        return Ok(workingHours);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkingHours([FromBody] CreateWorkingHoursDto dto)
    {
        var doctorId = GetCurrentDoctorId();

        var existing = await _db.Set<WorkingHours>()
            .FirstOrDefaultAsync(w => w.DoctorId == doctorId && w.DayOfWeek == (DayOfWeek)dto.DayOfWeek);

        if (existing != null)
        {
            existing.StartTime = TimeOnly.Parse(dto.StartTime);
            existing.EndTime = TimeOnly.Parse(dto.EndTime);
            existing.SlotDuration = dto.SlotDuration;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Horaires mis à jour avec succès" });
        }

        var workingHours = new WorkingHours
        {
            DoctorId = doctorId,
            DayOfWeek = (DayOfWeek)dto.DayOfWeek,
            StartTime = TimeOnly.Parse(dto.StartTime),
            EndTime = TimeOnly.Parse(dto.EndTime),
            SlotDuration = dto.SlotDuration,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<WorkingHours>().Add(workingHours);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Horaires ajoutés avec succès" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkingHours(int id, [FromBody] UpdateWorkingHoursDto dto)
    {
        var doctorId = GetCurrentDoctorId();
        var workingHours = await _db.Set<WorkingHours>()
            .FirstOrDefaultAsync(w => w.Id == id && w.DoctorId == doctorId);

        if (workingHours == null)
            return NotFound(new { message = "Horaires non trouvés" });

        if (!string.IsNullOrEmpty(dto.StartTime))
            workingHours.StartTime = TimeOnly.Parse(dto.StartTime);
        if (!string.IsNullOrEmpty(dto.EndTime))
            workingHours.EndTime = TimeOnly.Parse(dto.EndTime);
        if (dto.SlotDuration.HasValue)
            workingHours.SlotDuration = dto.SlotDuration.Value;
        if (dto.IsActive.HasValue)
            workingHours.IsActive = dto.IsActive.Value;

        workingHours.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Horaires mis à jour" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkingHours(int id)
    {
        var doctorId = GetCurrentDoctorId();
        var workingHours = await _db.Set<WorkingHours>()
            .FirstOrDefaultAsync(w => w.Id == id && w.DoctorId == doctorId);

        if (workingHours == null)
            return NotFound(new { message = "Horaires non trouvés" });

        _db.Set<WorkingHours>().Remove(workingHours);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Horaires supprimés" });
    }

    private static string GetDayName(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            1 => "Lundi",
            2 => "Mardi",
            3 => "Mercredi",
            4 => "Jeudi",
            5 => "Vendredi",
            6 => "Samedi",
            7 => "Dimanche",
            _ => ""
        };
    }
}
