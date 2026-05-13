using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactMessagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ContactMessagesController(AppDbContext context)
    {
        _context = context;
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
                UserId = admin.Id.ToString(),
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
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
