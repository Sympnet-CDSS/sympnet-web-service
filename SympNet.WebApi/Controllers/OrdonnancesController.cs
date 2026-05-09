using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor,Admin")]
public class OrdonnancesController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdonnancesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ordonnance>>> GetOrdonnances()
    {
        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        return await _context.Ordonnances
            .Include(o => o.Patient)
            .Where(o => o.DoctorId == doctorId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ordonnance>> GetOrdonnance(int id)
    {
        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        var ordonnance = await _context.Ordonnances
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == id && o.DoctorId == doctorId);

        if (ordonnance == null)
            return NotFound();

        return ordonnance;
    }

    [HttpPost]
    public async Task<ActionResult<Ordonnance>> PostOrdonnance(OrdonnanceCreateDto dto)
    {
        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        var ordonnance = new Ordonnance
        {
            DoctorId = doctorId,
            PatientId = dto.PatientId,
            ConsultationId = dto.ConsultationId,
            Diagnosis = dto.Diagnosis,
            MedicationsJson = dto.MedicationsJson,
            Notes = dto.Notes,
            HasAIAlerts = dto.HasAIAlerts,
            AIAlertsJson = dto.AIAlertsJson,
            CreatedAt = DateTime.UtcNow,
            OrdonnanceCode = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}"
        };

        _context.Ordonnances.Add(ordonnance);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrdonnance), new { id = ordonnance.Id }, ordonnance);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrdonnance(int id, OrdonnanceCreateDto dto)
    {
        if (id != dto.Id)
            return BadRequest();

        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        var ordonnance = await _context.Ordonnances.FindAsync(id);
        if (ordonnance == null)
            return NotFound();

        if (ordonnance.DoctorId != doctorId)
            return Forbid();

        ordonnance.PatientId = dto.PatientId;
        ordonnance.ConsultationId = dto.ConsultationId;
        ordonnance.Diagnosis = dto.Diagnosis;
        ordonnance.MedicationsJson = dto.MedicationsJson;
        ordonnance.Notes = dto.Notes;
        ordonnance.HasAIAlerts = dto.HasAIAlerts;
        ordonnance.AIAlertsJson = dto.AIAlertsJson;

        _context.Entry(ordonnance).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrdonnanceExists(id))
                return NotFound();
            else
                throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrdonnance(int id)
    {
        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        var ordonnance = await _context.Ordonnances.FindAsync(id);
        if (ordonnance == null || ordonnance.DoctorId != doctorId)
            return NotFound();

        _context.Ordonnances.Remove(ordonnance);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool OrdonnanceExists(int id)
    {
        return _context.Ordonnances.Any(e => e.Id == id);
    }

    private int GetCurrentDoctorId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == userId);
            return doctor?.Id ?? 0;
        }
        return 0;
    }
}

public class OrdonnanceCreateDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? ConsultationId { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string MedicationsJson { get; set; } = "[]";
    public string? Notes { get; set; }
    public bool HasAIAlerts { get; set; }
    public string? AIAlertsJson { get; set; }
}
