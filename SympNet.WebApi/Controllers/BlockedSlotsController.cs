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
public class BlockedSlotsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BlockedSlotsController(AppDbContext db)
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

    // GET: api/blockedslots
    [HttpGet]
    public async Task<IActionResult> GetBlockedSlots([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var doctorId = GetCurrentDoctorId();
        var query = _db.Set<BlockedSlot>().Where(b => b.DoctorId == doctorId);
        
        if (startDate.HasValue)
            query = query.Where(b => b.StartDateTime >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(b => b.StartDateTime <= endDate.Value);
        
        var blockedSlots = await query
            .OrderBy(b => b.StartDateTime)
            .Select(b => new BlockedSlotDto
            {
                Id = b.Id,
                StartDateTime = b.StartDateTime,
                EndDateTime = b.EndDateTime,
                Reason = b.Reason,
                IsRecurring = b.IsRecurring
            })
            .ToListAsync();

        return Ok(blockedSlots);
    }

    // POST: api/blockedslots
    [HttpPost]
    public async Task<IActionResult> CreateBlockedSlot([FromBody] CreateBlockedSlotDto dto)
    {
        var doctorId = GetCurrentDoctorId();
        
        // Vérifier qu'il n'y a pas de rendez-vous sur ce créneau
        var conflictingAppointments = await _db.Appointments
            .Where(a => a.DoctorId == doctorId && 
                       a.DateTime >= dto.StartDateTime && 
                       a.DateTime < dto.EndDateTime &&
                       a.Status != "Annulé")
            .AnyAsync();
        
        if (conflictingAppointments)
            return BadRequest(new { message = "Impossible de bloquer ce créneau : des rendez-vous sont déjà programmés" });

        var blockedSlot = new BlockedSlot
        {
            DoctorId = doctorId,
            StartDateTime = dto.StartDateTime,
            EndDateTime = dto.EndDateTime,
            Reason = dto.Reason,
            IsRecurring = dto.IsRecurring,
            RecurrencePattern = dto.RecurrencePattern,
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<BlockedSlot>().Add(blockedSlot);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Créneau bloqué avec succès", id = blockedSlot.Id });
    }

    // DELETE: api/blockedslots/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBlockedSlot(int id)
    {
        var doctorId = GetCurrentDoctorId();
        var blockedSlot = await _db.Set<BlockedSlot>()
            .FirstOrDefaultAsync(b => b.Id == id && b.DoctorId == doctorId);
        
        if (blockedSlot == null)
            return NotFound(new { message = "Créneau non trouvé" });

        _db.Set<BlockedSlot>().Remove(blockedSlot);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Créneau débloqué" });
    }
}