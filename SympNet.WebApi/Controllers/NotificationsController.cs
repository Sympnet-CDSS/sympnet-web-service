// Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Hubs;
using SympNet.WebApi.Models;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim)) throw new UnauthorizedAccessException();
        return Guid.Parse(claim);
    }


    // PATCH api/notifications/{id}/read — marquer une notif comme lue
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        var notif  = await _db.DoctorNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.DoctorUserId == userId);

        if (notif == null) return NotFound();

        notif.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    // PATCH api/notifications/read-all — tout marquer comme lu
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var notifs = await _db.DoctorNotifications
            .Where(n => n.DoctorUserId == userId && !n.IsRead)
            .ToListAsync();

        notifs.ForEach(n => n.IsRead = true);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // DELETE api/notifications — supprimer toutes les notifs
    [HttpDelete]
    public async Task<IActionResult> ClearAll()
    {
        var userId = GetCurrentUserId();
        var notifs = await _db.DoctorNotifications
            .Where(n => n.DoctorUserId == userId)
            .ToListAsync();

        _db.DoctorNotifications.RemoveRange(notifs);
        await _db.SaveChangesAsync();
        return Ok();
    }
    [HttpGet]
public async Task<IActionResult> GetMyNotifications()
{
    try
    {
        var userId = GetCurrentUserId();
        Console.WriteLine($"[Notifs] GetMyNotifications pour userId={userId}");

        var notifs = await _db.DoctorNotifications
            .Where(n => n.DoctorUserId == userId)
            .OrderByDescending(n => n.SentAt)
            .Take(50)
            .ToListAsync();

        Console.WriteLine($"[Notifs] {notifs.Count} notifications trouvées");
        return Ok(notifs);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Notifs] ERREUR: {ex.Message}");
        Console.WriteLine($"[Notifs] STACK: {ex.StackTrace}");
        return StatusCode(500, new { message = ex.Message });
    }
}
}