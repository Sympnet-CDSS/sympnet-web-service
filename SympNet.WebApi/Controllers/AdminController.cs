using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    // US-01: GET tous les utilisateurs
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.IsActive,
                u.FullName,
                u.CreatedAt,
                u.LastLoginAt
            })
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return Ok(users);
    }

    // US-01: GET un utilisateur par ID
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Role,
            user.IsActive,
            user.FullName,
            user.CreatedAt,
            user.LastLoginAt
        });
    }

    // US-01: POST créer un utilisateur (médecin ou patient)
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // Vérifie si l'email existe déjà
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Cet email est déjà utilisé" });

        // Vérifie que le rôle est valide
        if (dto.Role != "Admin" && dto.Role != "Doctor" && dto.Role != "Patient")
            return BadRequest(new { message = "Rôle invalide. Utilisez Admin, Doctor ou Patient" });

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            FullName = dto.FullName,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Role,
            user.IsActive,
            user.FullName,
            message = "Utilisateur créé avec succès"
        });
    }

    // US-01: PUT modifier un utilisateur
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;

        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        if (!string.IsNullOrEmpty(dto.Role))
        {
            if (dto.Role != "Admin" && dto.Role != "Doctor" && dto.Role != "Patient")
                return BadRequest(new { message = "Rôle invalide" });
            user.Role = dto.Role;
        }

        if (!string.IsNullOrEmpty(dto.FullName))
            user.FullName = dto.FullName;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Role,
            user.IsActive,
            user.FullName,
            message = "Utilisateur modifié avec succès"
        });
    }

    // US-01: DELETE supprimer un utilisateur
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        // Empêche la suppression du dernier Admin
        if (user.Role == "Admin")
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == "Admin");
            if (adminCount <= 1)
                return BadRequest(new { message = "Impossible de supprimer le dernier administrateur" });
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Utilisateur supprimé avec succès" });
    }

    // US-03: Activer/Désactiver un utilisateur
    [HttpPatch("users/{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        // Empêche la désactivation du dernier Admin
        if (user.Role == "Admin" && user.IsActive)
        {
            var activeAdminCount = await _db.Users.CountAsync(u => u.Role == "Admin" && u.IsActive);
            if (activeAdminCount <= 1)
                return BadRequest(new { message = "Impossible de désactiver le dernier administrateur actif" });
        }

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.IsActive,
            message = user.IsActive ? "Compte activé" : "Compte désactivé"
        });
    }

    // US-02: Statistiques globales
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalDoctors = await _db.Users.CountAsync(u => u.Role == "Doctor");
        var totalPatients = await _db.Users.CountAsync(u => u.Role == "Patient");
        var totalAdmins = await _db.Users.CountAsync(u => u.Role == "Admin");
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive);
        var inactiveUsers = totalUsers - activeUsers;

        // Stats par mois (pour le graphique)
        var last6Months = Enumerable.Range(0, 6)
            .Select(i => DateTime.UtcNow.AddMonths(-i).Date)
            .Select(date => new
            {
                Month = date.ToString("MMM yyyy"),
                Count = _db.Users.Count(u => u.CreatedAt.Year == date.Year && u.CreatedAt.Month == date.Month)
            })
            .OrderBy(x => x.Month)
            .ToList();

        return Ok(new
        {
            totalUsers,
            totalDoctors,
            totalPatients,
            totalAdmins,
            activeUsers,
            inactiveUsers,
            monthlyStats = last6Months
        });
    }
}

// DTOs
public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Patient";
    public string? FullName { get; set; }
}

public class UpdateUserDto
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public string? FullName { get; set; }
}
