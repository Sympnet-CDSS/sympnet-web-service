using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "Admin")]
public class AdminNotificationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Services.EmailService _emailService;

    public AdminNotificationsController(AppDbContext context, Services.EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    private Guid GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idStr, out var guid) ? guid : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetMyNotifications()
    {
        var userId = GetCurrentUserId();
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyRequest request)
    {
        var userId = GetCurrentUserId();
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null) return NotFound();

        if (string.IsNullOrEmpty(request.Email))
            return BadRequest(new { message = "Email destinataire manquant" });

        try
        {
            await _emailService.SendContactReplyEmailAsync(
                request.Email, 
                request.FirstName ?? "Utilisateur", 
                notification.Message, 
                request.ReplyMessage
            );

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Email envoyé avec succès" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erreur lors de l'envoi: {ex.Message}" });
        }
    }

    public class ReplyRequest
    {
        public string Email { get; set; } = "";
        public string? FirstName { get; set; }
        public string ReplyMessage { get; set; } = "";
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(int id)
    {
        var userId = GetCurrentUserId();
        var notification = await _context.Notifications
            .Include(n => n.Patient)
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null) return NotFound();

        // If it's a contact message notification, try to find the contact message
        ContactMessage? contactMessage = null;
        if (notification.Title.Contains("message de contact") && !string.IsNullOrEmpty(notification.ActionUrl))
        {
            // Try to extract ID from ActionUrl like "/admin/notifications/12"
            var parts = notification.ActionUrl.Split('/');
            if (parts.Length > 0 && int.TryParse(parts.Last(), out var contactId))
            {
                contactMessage = await _context.ContactMessages.FindAsync(contactId);
            }
        }

        return Ok(new {
            notification.Id,
            notification.Title,
            notification.Message,
            notification.CreatedAt,
            notification.IsRead,
            notification.ActionUrl,
            notification.Type,
            Patient = notification.Patient != null ? new {
                notification.Patient.FirstName,
                notification.Patient.LastName,
                notification.Patient.PhoneNumber,
                notification.Patient.DateOfBirth,
                notification.Patient.Address,
                notification.Patient.MedicalHistory
            } : null,
            ContactMessage = contactMessage != null ? new {
                contactMessage.FirstName,
                contactMessage.LastName,
                contactMessage.Email,
                contactMessage.Phone,
                contactMessage.Topic,
                contactMessage.Message
            } : null,
            NewsletterEmail = notification.Title.ToLower().Contains("newsletter") ? notification.Message.Split(' ').LastOrDefault(s => s.Contains("@")) : null
        });
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null) return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        notifications.ForEach(n => n.IsRead = true);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null) return NotFound();

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
