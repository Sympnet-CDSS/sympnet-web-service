using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public UploadController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost("photo")]
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier n'a été fourni." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Type de fichier non autorisé. Utilisez JPG, PNG ou GIF." });

        if (file.Length > 5 * 1024 * 1024) // 5 MB max
            return BadRequest(new { message = "Le fichier est trop volumineux (5 Mo maximum)." });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avatars");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        // Supprimer l'ancienne photo si elle existe
        if (!string.IsNullOrEmpty(user.PhotoUrl))
        {
            var oldFileName = Path.GetFileName(new Uri(user.PhotoUrl).LocalPath);
            var oldFilePath = Path.Combine(uploadsFolder, oldFileName);
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }
        }

        var photoUrl = $"{Request.Scheme}://{Request.Host}/uploads/avatars/{uniqueFileName}";
        user.PhotoUrl = photoUrl;
        await _db.SaveChangesAsync();

        return Ok(new { photoUrl = photoUrl, message = "Photo mise à jour avec succès" });
    }
}