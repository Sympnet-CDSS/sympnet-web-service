using MailKit.Net.Smtp;
using MimeKit;

namespace SympNet.WebApi.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // ── WEB : Credentials médecin ─────────────────────────────────────────
    public async Task SendDoctorCredentialsAsync(string toEmail, string firstName, string tempPassword)
    {
        var frontendUrl = _config["App:FrontendUrl"] ?? "http://localhost:5002";
        var loginUrl = $"{frontendUrl}/login";

        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 550px; margin: 0 auto; background: #fff; border-radius: 16px; border: 1px solid #DFF0EF; overflow: hidden;'>
    <div style='background: linear-gradient(135deg, #084E4B, #1A9E97); padding: 32px 40px; text-align: center;'>
        <h1 style='color: #fff; font-size: 24px; margin: 0;'>SympNet</h1>
        <p style='color: rgba(255,255,255,0.7); font-size: 12px; margin: 5px 0 0;'>Système Intelligent de Support Décisionnel Clinique</p>
    </div>
    <div style='padding: 32px 40px;'>
        <h2 style='color: #0B2D2C; font-size: 20px; margin: 0 0 12px;'>Bienvenue Dr. {firstName} !</h2>
        <p style='color: #5E8584; font-size: 14px; line-height: 1.6; margin: 0 0 20px;'>
            Votre compte médecin SympNet a été créé par l'administrateur.
        </p>
        <table style='background: #F0FBFA; border-radius: 12px; padding: 16px 20px; border: 1px solid #DFF0EF; width: 100%; margin-bottom: 24px;'>
            <tr><td style='color: #5E8584; font-size: 13px; padding: 6px 0;'>Email</td><td style='color: #0B2D2C; font-weight: 700; font-size: 13px;'>{toEmail}</td></tr>
            <tr><td style='color: #5E8584; font-size: 13px; padding: 6px 0;'>Mot de passe temporaire</td><td style='color: #0B2D2C; font-weight: 700; font-family: monospace; font-size: 14px; letter-spacing: 1px;'>{tempPassword}</td></tr>
        </table>
        <p style='color: #E05A00; font-size: 13px; font-weight: 600; margin: 0 0 20px;'>
            ⚠️ Veuillez changer votre mot de passe dès votre première connexion.
        </p>
        <div style='text-align: center; margin: 28px 0;'>
            <a href='{loginUrl}' style='background: linear-gradient(135deg, #0D6E6A, #3ABFB8); color: white; padding: 12px 32px; border-radius: 10px; text-decoration: none; font-weight: 600; display: inline-block;'>
                Se connecter
            </a>
        </div>
    </div>
    <div style='background: #F0FBFA; padding: 16px 40px; text-align: center; border-top: 1px solid #DFF0EF;'>
        <p style='color: #9DBDBC; font-size: 11px; margin: 0;'>© 2026 SympNet — Tous droits réservés</p>
    </div>
</div>";

        await SendEmailAsync(toEmail, "Vos identifiants SympNet — Compte Médecin", body);
    }

    // ── WEB : Reset password par lien ─────────────────────────────────────
    public async Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink)
    {
        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 550px; margin: 0 auto; background: #fff; border-radius: 16px; border: 1px solid #DFF0EF; overflow: hidden;'>
    <div style='background: linear-gradient(135deg, #084E4B, #1A9E97); padding: 32px 40px; text-align: center;'>
        <h1 style='color: #fff; font-size: 24px; margin: 0;'>SympNet</h1>
        <p style='color: rgba(255,255,255,0.7); font-size: 12px; margin: 5px 0 0;'>Réinitialisation du mot de passe</p>
    </div>
    <div style='padding: 32px 40px;'>
        <h2 style='color: #0B2D2C; font-size: 20px; margin: 0 0 12px;'>Bonjour {firstName} !</h2>
        <p style='color: #5E8584; font-size: 14px; line-height: 1.6; margin: 0 0 20px;'>
            Vous avez demandé la réinitialisation de votre mot de passe.
        </p>
        <p style='color: #5E8584; font-size: 14px; line-height: 1.6; margin: 0 0 20px;'>
            Cliquez sur le bouton ci-dessous pour créer un nouveau mot de passe.
            Ce lien est valable <strong>1 heure</strong>.
        </p>
        <div style='text-align: center; margin: 28px 0;'>
            <a href='{resetLink}' style='background: linear-gradient(135deg, #0D6E6A, #3ABFB8); color: white; padding: 12px 32px; border-radius: 10px; text-decoration: none; font-weight: 600; display: inline-block;'>
                Réinitialiser mon mot de passe
            </a>
        </div>
        <p style='color: #9DBDBC; font-size: 12px; line-height: 1.6; margin: 0;'>
            Si vous n'avez pas demandé cette réinitialisation, ignorez cet email.
        </p>
    </div>
    <div style='background: #F0FBFA; padding: 16px 40px; text-align: center; border-top: 1px solid #DFF0EF;'>
        <p style='color: #9DBDBC; font-size: 11px; margin: 0;'>© 2026 SympNet — Tous droits réservés</p>
    </div>
</div>";

        await SendEmailAsync(toEmail, "Réinitialisation de votre mot de passe SympNet", body);
    }

    // ── ANDROID + WEB : Vérification email par code ───────────────────────
    public async Task SendVerificationCodeAsync(string toEmail, string code)
    {
        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 550px; margin: 0 auto; background: #fff; border-radius: 16px; border: 1px solid #DFF0EF; overflow: hidden;'>
    <div style='background: linear-gradient(135deg, #084E4B, #1A9E97); padding: 32px 40px; text-align: center;'>
        <h1 style='color: #fff; font-size: 24px; margin: 0;'>SympNet</h1>
    </div>
    <div style='padding: 32px 40px; text-align: center;'>
        <h2 style='color: #0B2D2C;'>Code de vérification</h2>
        <p style='color: #5E8584; font-size: 14px;'>Utilisez ce code pour confirmer votre inscription. Il expire dans <strong>10 minutes</strong>.</p>
        <div style='background: #F0FBFA; border-radius: 12px; padding: 20px; margin: 24px 0; border: 1px solid #DFF0EF;'>
            <span style='font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #084E4B; font-family: monospace;'>{code}</span>
        </div>
        <p style='color: #9DBDBC; font-size: 12px;'>Si vous n'avez pas créé de compte, ignorez cet email.</p>
    </div>
    <div style='background: #F0FBFA; padding: 16px 40px; text-align: center; border-top: 1px solid #DFF0EF;'>
        <p style='color: #9DBDBC; font-size: 11px; margin: 0;'>© 2026 SympNet — Tous droits réservés</p>
    </div>
</div>";

        await SendEmailAsync(toEmail, "Votre code de vérification SympNet", body);
    }

    // ── ANDROID : Reset password par code ────────────────────────────────
    public async Task SendPasswordResetCodeAsync(string toEmail, string code)
    {
        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 550px; margin: 0 auto; background: #fff; border-radius: 16px; border: 1px solid #DFF0EF; overflow: hidden;'>
    <div style='background: linear-gradient(135deg, #084E4B, #1A9E97); padding: 32px 40px; text-align: center;'>
        <h1 style='color: #fff; font-size: 24px; margin: 0;'>SympNet</h1>
    </div>
    <div style='padding: 32px 40px; text-align: center;'>
        <h2 style='color: #0B2D2C;'>Réinitialisation du mot de passe</h2>
        <p style='color: #5E8584; font-size: 14px;'>Utilisez ce code pour réinitialiser votre mot de passe. Il expire dans <strong>10 minutes</strong>.</p>
        <div style='background: #F0FBFA; border-radius: 12px; padding: 20px; margin: 24px 0; border: 1px solid #DFF0EF;'>
            <span style='font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #084E4B; font-family: monospace;'>{code}</span>
        </div>
        <p style='color: #9DBDBC; font-size: 12px;'>Si vous n'avez pas demandé cette réinitialisation, ignorez cet email.</p>
    </div>
    <div style='background: #F0FBFA; padding: 16px 40px; text-align: center; border-top: 1px solid #DFF0EF;'>
        <p style='color: #9DBDBC; font-size: 11px; margin: 0;'>© 2026 SympNet — Tous droits réservés</p>
    </div>
</div>";

        await SendEmailAsync(toEmail, "Code de réinitialisation SympNet", body);
    }

    // ── Méthode commune d'envoi ───────────────────────────────────────────
    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var smtpUser = _config["Email:Username"] ?? throw new Exception("Email:Username not configured");
        var smtpPass = _config["Email:Password"] ?? throw new Exception("Email:Password not configured");
        var fromEmail = _config["Email:From"] ?? smtpUser;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("SympNet", fromEmail));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpUser, smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}