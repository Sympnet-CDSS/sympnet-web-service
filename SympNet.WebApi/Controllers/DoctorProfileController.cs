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

        return Ok(new
        {
            doctor.Id,
            doctor.User.Email,
            doctor.FirstName,
            doctor.LastName,
            doctor.Speciality,
            doctor.LicenseNumber,
            doctor.Address,
            doctor.Latitude,
            doctor.Longitude,
            doctor.TotalConsultations,
            doctor.TotalPatients,
            IsActive = doctor.User.IsActive
        });
    }

    // GET: api/doctorprofile/stats
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
        
        return Ok(new
        {
            totalPatients,
            todayConsultations,
            totalConsultations
        });
    }
}