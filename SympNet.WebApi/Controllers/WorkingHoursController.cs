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

    private Guid GetCurrentDoctorId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException();
        
        var doctor = _db.Doctors.FirstOrDefault(d => d.UserId == Guid.Parse(userIdClaim));
        return doctor?.UserId ?? Guid.Parse(userIdClaim);
    }

    // GET: api/workinghours
    [HttpGet]
    public async Task<IActionResult> GetWorkingHours()
    {
        var doctorId = GetCurrentDoctorId();
        
        var workingHours = await _db.Set<WorkingHours>()
            .Where(w => w.DoctorId == doctorId)
            .OrderBy(w => w.DayOfWeek)
            .Select(w => new WorkingHoursDto
            {
                Id = w.Id,
                DayOfWeek = (int)w.DayOfWeek,
                DayName = GetDayName(w.DayOfWeek),
                StartTime = w.StartTime.ToString(),
                EndTime = w.EndTime.ToString(),
                SlotDuration = w.SlotDuration,
                IsActive = w.IsActive
            })
            .ToListAsync();

        return Ok(workingHours);
    }

    // POST: api/workinghours
    [HttpPost]
    public async Task<IActionResult> CreateWorkingHours([FromBody] CreateWorkingHoursDto dto)
    {
        var doctorId = GetCurrentDoctorId();
        
        // Vérifier si déjà existant
        var existing = await _db.Set<WorkingHours>()
            .FirstOrDefaultAsync(w => w.DoctorId == doctorId && w.DayOfWeek == (DayOfWeek)dto.DayOfWeek);
        
        if (existing != null)
            return BadRequest(new { message = "Les horaires pour ce jour existent déjà" });

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

    // PUT: api/workinghours/{id}
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

    // DELETE: api/workinghours/{id}
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

    private string GetDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Lundi",
            DayOfWeek.Tuesday => "Mardi",
            DayOfWeek.Wednesday => "Mercredi",
            DayOfWeek.Thursday => "Jeudi",
            DayOfWeek.Friday => "Vendredi",
            DayOfWeek.Saturday => "Samedi",
            DayOfWeek.Sunday => "Dimanche",
            _ => ""
        };
    }
}