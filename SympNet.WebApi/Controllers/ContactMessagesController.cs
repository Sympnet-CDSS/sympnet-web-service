using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactMessagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Services.EmailService _emailService;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.NotificationHub> _hubContext;

    public ContactMessagesController(
        AppDbContext context, 
        Services.EmailService emailService,
        Microsoft.AspNetCore.SignalR.IHubContext<Hubs.NotificationHub> hubContext)
    {
        _context = context;
        _emailService = emailService;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContactMessage message)
    {
        message.CreatedAt = DateTime.UtcNow;
        message.IsRead = false;
        
        _context.ContactMessages.Add(message);
        await _context.SaveChangesAsync();

        // Create notification for Admin
        var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
        foreach (var admin in admins)
        {
            var notification = new Notification
            {
                UserId = admin.Id,
                Title = "Nouveau message de contact",
                Message = $"Nouveau message de {message.FirstName} {message.LastName} sur le sujet : {message.Topic}",
                Type = NotificationType.General,
                ActionUrl = $"/admin/notifications/{message.Id}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }
        
        await _context.SaveChangesAsync();

        // Notify admins in real-time
        await _hubContext.Clients.Group("admins").SendAsync("ReceiveNotification");

        return Ok(new { message = "Message envoyé avec succès" });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContactMessage>>> GetAll()
    {
        return await _context.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContactMessage>> GetById(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null) return NotFound();
        return message;
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null) return NotFound();

        message.IsRead = true;
        
        // Envoi automatique d'un email de confirmation de traitement
        try 
        {
            await _emailService.SendProcessingConfirmationAsync(message.Email, message.FirstName);
        }
        catch (Exception ex) { Console.WriteLine($"Email processing failed: {ex.Message}"); }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] Dtos.ReplyRequest request)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null) return NotFound();

        // Envoi de l'email via le service
        try 
        {
            await _emailService.SendContactReplyEmailAsync(
                message.Email, 
                message.FirstName, 
                message.Message, 
                request.Message
            );
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        // Marquer comme lu après réponse
        message.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Réponse envoyée avec succès" });
    }
}
