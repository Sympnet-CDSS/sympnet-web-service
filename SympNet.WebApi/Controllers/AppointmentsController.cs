// Controllers/AppointmentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Dtos;
using SympNet.WebApi.Hubs;
using SympNet.WebApi.Models;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext                 _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IHubContext<ChatHub>         _chatHub; // ✅ ajouté

    public AppointmentsController(
        AppDbContext db,
        IHubContext<NotificationHub> hub,
        IHubContext<ChatHub> chatHub) // ✅ ajouté
    {
        _db      = db;
        _hub     = hub;
        _chatHub = chatHub; // ✅ ajouté
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim)) throw new UnauthorizedAccessException();
        return Guid.Parse(claim);
    }

    // ── GET api/appointments ──────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Patient,Doctor")]
    public async Task<IActionResult> GetMyAppointments()
    {
        var userId = GetCurrentUserId();

        var appointments = await _db.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == userId)
            .OrderBy(a => a.DateTime)
            .Select(a => new AppointmentDto
            {
                Id               = a.Id,
                DoctorId         = a.DoctorId,
                DoctorName       = a.Doctor != null ? $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}" : "Docteur",
                DoctorSpeciality = a.Doctor != null ? a.Doctor.Speciality : "Généraliste",
                DoctorAddress    = a.Doctor != null ? a.Doctor.Address    : "Adresse non renseignée",
                DateTime         = a.DateTime,
                Status           = a.Status,
                Notes            = a.Notes,
                Type             = a.Type ?? "Consultation",
                IsUrgent         = a.IsUrgent,
                Reason           = a.Reason
            })
            .ToListAsync();

        return Ok(appointments);
    }

    // ── GET api/appointments/{id} ─────────────────────────────────────────────
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        var userId      = GetCurrentUserId();
        var appointment = await _db.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound(new { message = "Rendez-vous non trouvé" });

        var isPatient = appointment.PatientId == userId;
        var isDoctor  = await _db.Doctors.AnyAsync(d => d.UserId == userId && d.Id == appointment.DoctorId);

        if (!isPatient && !isDoctor)
            return Unauthorized();

        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == appointment.PatientId);
        var patientAge = patient != null ? DateTime.Today.Year - patient.DateOfBirth.Year : 0;
        if (patient != null && patient.DateOfBirth.Date > DateTime.Today.AddYears(-patientAge)) patientAge--;

        return Ok(new AppointmentDto
        {
            Id                = appointment.Id,
            PatientId         = appointment.PatientId,
            PatientName       = appointment.Patient?.FullName ?? "",
            PatientEmail      = appointment.Patient?.Email ?? "",
            PatientPhone      = patient?.PhoneNumber ?? "",
            PatientAge        = patientAge,
            PatientLocation   = patient?.Address ?? "Tunis, Tunisie",
            PatientConditions = patient?.MedicalHistory ?? "Aucune condition",
            PatientGender     = patient?.Gender ?? "",
            PatientPhotoUrl   = appointment.Patient?.PhotoUrl ?? "",
            DoctorId          = appointment.DoctorId,
            DoctorName        = appointment.Doctor != null
                                    ? $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                                    : "Docteur",
            DoctorSpeciality  = appointment.Doctor?.Speciality ?? "Généraliste",
            DoctorAddress     = appointment.Doctor?.Address    ?? "Adresse non renseignée",
            DateTime          = appointment.DateTime,
            Status            = appointment.Status,
            Notes             = appointment.Notes,
            Type              = appointment.Type ?? "Consultation",
            IsUrgent          = appointment.IsUrgent,
            Reason            = appointment.Reason,
            CreatedAt         = appointment.CreatedAt
        });
    }

    // ── GET api/appointments/doctor/{doctorId} ────────────────────────────────
    [HttpGet("doctor/{doctorId}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetDoctorAppointments(int doctorId)
    {
        var userId = GetCurrentUserId();
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null || doctor.Id != doctorId)
            return Unauthorized();

        var appointments = await _db.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctorId)
            .OrderBy(a => a.DateTime)
            .Select(a => new
            {
                a.Id,
                PatientName  = a.Patient != null ? a.Patient.FullName : "Patient",
                PatientEmail = a.Patient != null ? a.Patient.Email    : "Email non renseigné",
                a.DateTime,
                a.Status,
                a.Notes
            })
            .ToListAsync();

        return Ok(appointments);
    }

    // ── GET api/appointments/confirmed ───────────────────────────────────────
    // ✅ Vérifie si un rendez-vous confirmé existe entre patient et docteur
    [HttpGet("confirmed")]
    [Authorize]
    public async Task<IActionResult> GetConfirmedAppointments(
        [FromQuery] Guid patientId,
        [FromQuery] int doctorId)
    {
        var appointments = await _db.Appointments
            .Where(a => a.PatientId == patientId
                     && a.DoctorId  == doctorId
                     && a.Status    == "Confirmé")
            .ToListAsync();

        return Ok(appointments);
    }

    // ── POST api/appointments ─────────────────────────────────────────────────
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        var userId = GetCurrentUserId();
        var role   = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Doctor")
        {
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return Unauthorized();

            if (string.IsNullOrEmpty(dto.PatientEmail))
                return BadRequest(new { message = "Email patient requis." });

            var patientUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.PatientEmail && u.Role == "Patient");

            if (patientUser == null)
                return BadRequest(new { message = "Patient introuvable avec cet email." });

            var appointment = new Appointment
            {
                PatientId = patientUser.Id,
                DoctorId  = doctor.Id,
                DateTime  = DateTime.SpecifyKind(dto.DateTime, DateTimeKind.Utc),
                Duration  = dto.Duration > 0 ? dto.Duration : 30,
                Type      = dto.Type   ?? "Consultation",
                Notes     = dto.Notes  ?? "",
                Reason    = dto.Reason ?? "",
                IsUrgent  = dto.IsUrgent,
                Status    = "Confirmé",
                CreatedAt = DateTime.UtcNow
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            // ✅ Créer conversation automatiquement quand docteur crée le RDV
            await CreateConversationIfNotExists(appointment);

            return Ok(new { message = "Rendez-vous créé avec succès", appointmentId = appointment.Id });
        }
        else
        {
            var doctor = await _db.Doctors.FindAsync(dto.DoctorId);
            if (doctor == null)
                return NotFound(new { message = "Médecin non trouvé" });

            var dateTimeUtc = DateTime.SpecifyKind(dto.DateTime, DateTimeKind.Utc);

            var conflict = await _db.Appointments
                .FirstOrDefaultAsync(a => a.DoctorId == dto.DoctorId && a.DateTime == dateTimeUtc);

            if (conflict != null)
                return BadRequest(new { message = "Ce créneau est déjà pris" });

            var appointment = new Appointment
            {
                PatientId = userId,
                DoctorId  = dto.DoctorId,
                DateTime  = dateTimeUtc,
                Type      = dto.Type   ?? "InPerson",
                IsUrgent  = dto.IsUrgent,
                Status    = "En attente",
                Notes     = dto.Notes  ?? "",
                Reason    = dto.Reason ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            await NotifyDoctorAsync(appointment, userId);

            return Ok(new { message = "Rendez-vous créé avec succès", appointmentId = appointment.Id });
        }
    }

    // ── PATCH api/appointments/{id}/status ───────────────────────────────────
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> UpdateAppointmentStatus(
        int id, [FromBody] UpdateAppointmentStatusDto dto)
    {
        var userId = GetCurrentUserId();

        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor) // ✅ ajouté pour accéder au nom du docteur
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound(new { message = "Rendez-vous non trouvé" });

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        if (doctor == null || doctor.Id != appointment.DoctorId)
            return Unauthorized(new { message = "Accès non autorisé" });

        var validStatuses = new[] { "Confirmé", "Annulé", "Terminé" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Statut invalide" });

        appointment.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.Notes))
            appointment.Notes = dto.Notes;

        await _db.SaveChangesAsync();

        // ✅ Créer la conversation automatiquement si le RDV est confirmé
        if (dto.Status == "Confirmé")
        {
            await CreateConversationIfNotExists(appointment);
        }

        try
        {
            var statusEmoji = dto.Status switch
            {
                "Confirmé" => "✅",
                "Annulé"   => "❌",
                "Terminé"  => "🏁",
                _          => "📋"
            };

            var title   = $"{statusEmoji} Rendez-vous {dto.Status}";
            var message = $"Votre rendez-vous du {appointment.DateTime:dd/MM/yyyy à HH:mm} a été {dto.Status.ToLower()} par votre médecin.";

            _db.PatientNotifications.Add(new PatientNotification
            {
                PatientUserId = appointment.PatientId,
                Title         = title,
                Message       = message,
                AppointmentId = appointment.Id,
                Status        = dto.Status,
                IsRead        = false,
                SentAt        = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            await _hub.Clients
                      .Group($"user_{appointment.PatientId}")
                      .SendAsync("ReceiveNotification", new
                      {
                          title,
                          message,
                          appointmentId = appointment.Id,
                          status        = dto.Status,
                          sentAt        = DateTime.UtcNow
                      });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] Erreur notification: {ex.Message}");
        }

        return Ok(new
        {
            message       = $"Rendez-vous {dto.Status} avec succès",
            appointmentId = appointment.Id,
            status        = dto.Status
        });
    }

    // ── Méthode privée — crée conversation + message bienvenue ───────────────
    private async Task CreateConversationIfNotExists(Appointment appointment)
    {
        try
        {
            // Recharger avec Doctor et Patient si pas déjà chargés
            if (appointment.Doctor == null || appointment.Patient == null)
            {
                appointment = await _db.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .FirstAsync(a => a.Id == appointment.Id);
            }

            var doctorUserId = appointment.Doctor?.UserId ?? Guid.Empty;

            var existingConv = await _db.Conversations
                .FirstOrDefaultAsync(c =>
                    c.DoctorId  == doctorUserId &&
                    c.PatientId == appointment.PatientId);

            if (existingConv != null) return; // déjà créée

            var conversation = new Conversation
            {
                DoctorId      = doctorUserId,
                PatientId     = appointment.PatientId,
                CreatedAt     = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync(); // ✅ SaveChanges avant d'utiliser conversation.Id

            var doctorName = appointment.Doctor != null
                ? $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                : "Votre médecin";

            var welcomeMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderId       = doctorUserId,
                SenderRole     = "doctor",
                Content        = $"Bonjour, votre rendez-vous du {appointment.DateTime:dd/MM/yyyy à HH:mm} est confirmé. N'hésitez pas à me contacter.",
                SentAt         = DateTime.UtcNow
            };
            _db.ChatMessages.Add(welcomeMessage);
            await _db.SaveChangesAsync();

            // ✅ Notifier le patient via SignalR
            await _chatHub.Clients
                .User(appointment.PatientId.ToString())
                .SendAsync("ConversationCreated",
                    doctorName,
                    appointment.DateTime.ToString("dd/MM/yyyy"));

            Console.WriteLine($"[Chat] Conversation créée entre docteur {appointment.DoctorId} et patient {appointment.PatientId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chat] Erreur création conversation: {ex.Message}");
        }
    }

    private async Task NotifyDoctorAsync(Appointment appointment, Guid patientUserId)
    {
        try
        {
            var patient     = await _db.Users.FindAsync(patientUserId);
            var patientName = patient?.FullName ?? "Un patient";

            var doctor = await _db.Doctors.FindAsync(appointment.DoctorId);
            if (doctor == null) return;

            var title   = "Nouveau rendez-vous 📅";
            var message = $"{patientName} a pris rendez-vous le {appointment.DateTime:dd/MM/yyyy à HH:mm}";

            var dbNotif = new DoctorNotification
            {
                DoctorUserId  = doctor.UserId,
                Title         = title,
                Message       = message,
                AppointmentId = appointment.Id,
                IsUrgent      = appointment.IsUrgent,
                IsRead        = false,
                SentAt        = DateTime.UtcNow
            };
            _db.DoctorNotifications.Add(dbNotif);
            await _db.SaveChangesAsync();

            await _hub.Clients
                      .Group($"doctor_user_{doctor.UserId}")
                      .SendAsync("ReceiveNotification", new
                      {
                          id            = dbNotif.Id,
                          type          = "NEW_APPOINTMENT",
                          title,
                          message,
                          appointmentId = appointment.Id,
                          isUrgent      = appointment.IsUrgent,
                          sentAt        = DateTime.UtcNow
                      });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] Erreur: {ex.Message}");
        }
    }
}
