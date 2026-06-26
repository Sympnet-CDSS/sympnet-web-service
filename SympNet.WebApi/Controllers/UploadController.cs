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

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            var base64String = Convert.ToBase64String(fileBytes);
            
            var photoUrl = $"data:{file.ContentType};base64,{base64String}";
            
            user.PhotoUrl = photoUrl;
            await _db.SaveChangesAsync();

            return Ok(new { photoUrl = photoUrl, message = "Photo mise à jour avec succès" });
        }
    }

    [HttpPost("blog-image")]
    public async Task<IActionResult> UploadBlogImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier n'a été fourni." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Type de fichier non autorisé." });

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "blog");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var photoUrl = $"{Request.Scheme}://{Request.Host}/uploads/blog/{uniqueFileName}";
        return Ok(new { url = photoUrl });
    }
}