using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SympNet.WebApi.Data;
using SympNet.WebApi.Dtos;
using SympNet.WebApi.Models;
using SympNet.WebApi.Services;
using System.Security.Cryptography;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;
    private readonly IConfiguration _config;

public PasswordController(AppDbContext db, EmailService emailService, IConfiguration config)
{
    _db = db;
    _emailService = emailService;
    _config = config;
}

  
    [HttpPost("forgot")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        
        //on ne révèle pas si l'email existe
        if (user == null)
        {
            return Ok(new { message = "Si cet email existe, vous recevrez un lien de réinitialisation." });
        }

        // Générer un token 
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        
        // Stocker le token et sa date d'expiration 
        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        
        await _db.SaveChangesAsync();

        // Construire le lien de réinitialisation
        var frontendUrl = _config["App:FrontendUrl"] ?? "http://localhost:5002";
        var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
        
        // Envoyer l'email
        try
        {
            var firstName = user.FullName?.Split(' ')[0] ?? "Utilisateur";
            await _emailService.SendPasswordResetEmailAsync(user.Email, firstName, resetLink);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur envoi email: {ex.Message}");
        }

        return Ok(new { message = "Si cet email existe, vous recevrez un lien de réinitialisation." });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        
        if (user == null)
        {
            return BadRequest(new { message = "Token invalide ou expiré." });
        }

        // Vérifier le token
        if (user.ResetToken != dto.Token || user.ResetTokenExpiry < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Token invalide ou expiré." });
        }

        // Valider le nouveau mot de passe
        if (dto.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "Le mot de passe doit contenir au moins 6 caractères." });
        }

        // Mettre à jour le mot de passe
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        
        await _db.SaveChangesAsync();

        return Ok(new { message = "Votre mot de passe a été réinitialisé avec succès." });
    }
}