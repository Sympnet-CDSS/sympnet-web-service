using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
using SympNet.WebApi.Services;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.NotificationHub> _hubContext;

    public NewsletterController(
        AppDbContext context, 
        EmailService emailService,
        Microsoft.AspNetCore.SignalR.IHubContext<Hubs.NotificationHub> hubContext)
    {
        _context = context;
        _emailService = emailService;
        _hubContext = hubContext;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email requis");

        var existing = await _context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email.ToLower() == request.Email.ToLower());

        if (existing != null)
        {
            if (existing.IsActive)
                return Ok(new { message = "Déjà inscrit" });
            
            existing.IsActive = true;
            existing.SubscribedAt = DateTime.UtcNow;
        }
        else
        {
            var subscriber = new NewsletterSubscriber
            {
                Email = request.Email,
                SubscribedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.NewsletterSubscribers.Add(subscriber);
        }

        await _context.SaveChangesAsync();
        
        // Create notification for Admin
        var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
        foreach (var admin in admins)
        {
            var notification = new Notification
            {
                UserId = admin.Id,
                Title = "Nouvel abonné Newsletter",
                Message = $"L'utilisateur {request.Email} s'est inscrit à la newsletter.",
                Type = NotificationType.General,
                ActionUrl = "/admin/newsletter",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }
        await _context.SaveChangesAsync();
        Console.WriteLine($"Newsletter: Created notifications for {admins.Count} admins.");

        // Notify admins in real-time
        await _hubContext.Clients.Group("admins").SendAsync("ReceiveNotification");
        Console.WriteLine("Newsletter: Sent real-time notification to 'admins' group.");

        try 
        {
            await _emailService.SendNewsletterConfirmationAsync(request.Email);
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Failed to send newsletter confirmation: {ex.Message}");
        }

        return Ok(new { message = "Inscription réussie" });
    }

    [HttpGet("admin/list")]
    public async Task<ActionResult<IEnumerable<NewsletterSubscriber>>> GetSubscribers(
        [FromQuery] string? search = null, 
        [FromQuery] bool? activeOnly = null)
    {
        var query = _context.NewsletterSubscribers.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => s.Email.Contains(search));
        }

        if (activeOnly.HasValue)
        {
            query = query.Where(s => s.IsActive == activeOnly.Value);
        }

        return await query.OrderByDescending(s => s.SubscribedAt).ToListAsync();
    }

    [HttpDelete("admin/{id}")]
    public async Task<IActionResult> DeleteSubscriber(int id)
    {
        var subscriber = await _context.NewsletterSubscribers.FindAsync(id);
        if (subscriber == null) return NotFound();

        _context.NewsletterSubscribers.Remove(subscriber);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public class SubscribeRequest
    {
        public string Email { get; set; } = "";
    }
}
