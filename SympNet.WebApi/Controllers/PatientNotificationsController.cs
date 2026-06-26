using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/patient-notifications")]
[Authorize(Roles = "Patient")]
public class PatientNotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientNotificationsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim)) throw new UnauthorizedAccessException();
        return Guid.Parse(claim);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetCurrentUserId();
        var notifs = await _db.PatientNotifications
            .Where(n => n.PatientUserId == userId)
            .OrderByDescending(n => n.SentAt)
            .Take(50)
            .ToListAsync();
        return Ok(notifs);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        var notif  = await _db.PatientNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.PatientUserId == userId);
        if (notif == null) return NotFound();
        notif.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var notifs = await _db.PatientNotifications
            .Where(n => n.PatientUserId == userId && !n.IsRead)
            .ToListAsync();
        notifs.ForEach(n => n.IsRead = true);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearAll()
    {
        var userId = GetCurrentUserId();
        var notifs = await _db.PatientNotifications
            .Where(n => n.PatientUserId == userId)
            .ToListAsync();
        _db.PatientNotifications.RemoveRange(notifs);
        await _db.SaveChangesAsync();
        return Ok();
    }
}