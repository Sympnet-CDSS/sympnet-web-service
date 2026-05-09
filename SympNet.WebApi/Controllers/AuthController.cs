using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
using SympNet.WebApi.Services;
using System.Security.Claims;
using SympNet.WebApi.Dtos;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly EmailService _email;

    public AuthController(AppDbContext db, JwtService jwt, EmailService email)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
    }

    // ── EXISTANT : Register web (sans vérification email) ─────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email deja utilise." });

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            FullName = dto.FullName,
            IsEmailVerified = true // web = pas de vérification
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponseDto(token, user.Email, user.Role, user.Id, user.FullName));
    }

    // ── EXISTANT : Login web ───────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email ou mot de passe incorrect." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Compte desactive." });

        if (!user.IsEmailVerified && user.Role == "Patient")
            return Unauthorized(new { message = "Email non vérifié. Vérifiez votre boîte mail." });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponseDto(token, user.Email, user.Role, user.Id, user.FullName));
    }

    // ── EXISTANT : Me ──────────────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _db.Users.FindAsync(Guid.Parse(userId));
        if (user == null) return NotFound();

        return Ok(new { user.Id, user.Email, user.Role, user.IsActive, user.FullName });
    }

    // ── ANDROID : Register avec vérification email ────────────────────────
    [HttpPost("register-mobile")]
    public async Task<IActionResult> RegisterMobile([FromBody] RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email deja utilise." });

        var code = new Random().Next(100000, 999999).ToString();

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            FullName = dto.FullName,
            VerificationCode = code,
            VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10),
            IsEmailVerified = false,
            IsActive = false
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _email.SendVerificationCodeAsync(dto.Email, code);

        return Ok(new { message = "Code de vérification envoyé à " + dto.Email });
    }

    // ── ANDROID : Vérification code email ─────────────────────────────────
    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return NotFound(new { message = "Utilisateur introuvable." });

        if (user.VerificationCode != dto.Code || user.VerificationCodeExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Code invalide ou expiré." });

        user.IsEmailVerified = true;
        user.IsActive = true;
        user.VerificationCode = null;
        user.VerificationCodeExpiry = null;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponseDto(token, user.Email, user.Role, user.Id, user.FullName));
    }

    // ── ANDROID : Forgot password par code ───────────────────────────────
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return NotFound(new { message = "Email introuvable." });

        var code = new Random().Next(100000, 999999).ToString();
        user.ResetToken = code;
        user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync();

        await _email.SendPasswordResetCodeAsync(dto.Email, code);

        return Ok(new { message = "Code envoyé à " + dto.Email });
    }

    // ── ANDROID : Vérifier code reset ─────────────────────────────────────
    [HttpPost("verify-reset-code")]
    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyCodeDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return NotFound(new { message = "Utilisateur introuvable." });

        if (user.ResetToken != dto.Code || user.ResetTokenExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Code invalide ou expiré." });

        return Ok(new { message = "Code valide." });
    }

    // ── ANDROID : Reset password par code ────────────────────────────────
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return NotFound(new { message = "Utilisateur introuvable." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Mot de passe réinitialisé." });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Patient";
    public string? FullName { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class VerifyCodeDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}


public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string? Token { get; set; }      
    public string? Code { get; set; }       
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? FullName { get; set; }

    public AuthResponseDto(string token, string email, string role, Guid userId, string? fullName)
    {
        Token = token;
        Email = email;
        Role = role;
        UserId = userId;
        FullName = fullName;
    }
}