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
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfileController(AppDbContext db)
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

    // GET: api/profile
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);
        
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        return Ok(new ProfileDto
        {
            Id = 0,
            Email = user.Email,
            FullName = user.FullName ?? "",
            PhotoUrl = user.PhotoUrl,
            IsActive = user.IsActive
        });
    }

    // PUT: api/profile/email
    [HttpPut("email")]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);
        
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        // Vérifier si l'email n'est pas déjà utilisé
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId))
            return BadRequest(new { message = "Cet email est déjà utilisé" });

        user.Email = dto.Email;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Email mis à jour avec succès" });
    }

    // PUT: api/profile/password
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);
        
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        // Vérifier l'ancien mot de passe
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mot de passe actuel incorrect" });

        // Valider le nouveau mot de passe
        if (dto.NewPassword.Length < 6)
            return BadRequest(new { message = "Le nouveau mot de passe doit contenir au moins 6 caractères" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Mot de passe mis à jour avec succès" });
    }

    // PUT: api/profile/photo
    [HttpPut("photo")]
public async Task<IActionResult> UpdatePhoto([FromBody] UpdatePhotoDto dto)
{
    var userId = GetCurrentUserId();
    var user = await _db.Users.FindAsync(userId);
    
    if (user == null)
        return NotFound(new { message = "Utilisateur non trouvé" });

    user.PhotoUrl = dto.PhotoBase64;
    await _db.SaveChangesAsync();

    return Ok(new { message = "Photo mise à jour avec succès" });
}
}