using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
using SympNet.WebApi.Hubs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using QRCoder;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdonnancesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hub;

    public OrdonnancesController(AppDbContext context, IHubContext<NotificationHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ordonnance>>> GetOrdonnances()
    {
        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        return await _context.Ordonnances
            .Include(o => o.Patient)
            .Where(o => o.DoctorId == doctorId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ordonnance>> GetOrdonnance(int id)
    {
        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        var ordonnance = await _context.Ordonnances
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == id && o.DoctorId == doctorId);

        if (ordonnance == null)
            return NotFound();

        return ordonnance;
    }

    [HttpPost]
    public async Task<ActionResult<Ordonnance>> PostOrdonnance(OrdonnanceCreateDto dto)
    {
        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        var ordonnance = new Ordonnance
        {
            DoctorId = doctorId,
            PatientId = dto.PatientId,
            ConsultationId = dto.ConsultationId,
            Diagnosis = dto.Diagnosis,
            MedicationsJson = dto.MedicationsJson,
            Notes = dto.Notes,
            HasAIAlerts = dto.HasAIAlerts,
            AIAlertsJson = dto.AIAlertsJson,
            CreatedAt = DateTime.UtcNow,
            OrdonnanceCode = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}"
        };

        _context.Ordonnances.Add(ordonnance);
        await _context.SaveChangesAsync();

        // ── NOTIFIER LE PATIENT ──
        try
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == dto.PatientId);
            if (patient != null)
            {
                var title = "Nouvelle Ordonnance 💊";
                var message = "Votre médecin vous a prescrit une nouvelle ordonnance.";

                _context.PatientNotifications.Add(new PatientNotification
                {
                    PatientUserId = patient.UserId,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    SentAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                await _hub.Clients
                    .Group($"user_{patient.UserId}")
                    .SendAsync("ReceiveNotification", new
                    {
                        title,
                        message,
                        sentAt = DateTime.UtcNow
                    });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] Erreur notification ordonnance: {ex.Message}");
        }

        return CreatedAtAction(nameof(GetOrdonnance), new { id = ordonnance.Id }, ordonnance);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrdonnance(int id, OrdonnanceCreateDto dto)
    {
        if (id != dto.Id)
            return BadRequest();

        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        var ordonnance = await _context.Ordonnances.FindAsync(id);
        if (ordonnance == null)
            return NotFound();

        if (ordonnance.DoctorId != doctorId)
            return Forbid();

        ordonnance.PatientId = dto.PatientId;
        ordonnance.ConsultationId = dto.ConsultationId;
        ordonnance.Diagnosis = dto.Diagnosis;
        ordonnance.MedicationsJson = dto.MedicationsJson;
        ordonnance.Notes = dto.Notes;
        ordonnance.HasAIAlerts = dto.HasAIAlerts;
        ordonnance.AIAlertsJson = dto.AIAlertsJson;

        _context.Entry(ordonnance).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrdonnanceExists(id))
                return NotFound();
            else
                throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrdonnance(int id)
    {
        var doctorId = GetCurrentDoctorId();
        if (doctorId == 0) return Unauthorized();

        var ordonnance = await _context.Ordonnances.FindAsync(id);
        if (ordonnance == null || ordonnance.DoctorId != doctorId)
            return NotFound();

        _context.Ordonnances.Remove(ordonnance);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool OrdonnanceExists(int id)
    {
        return _context.Ordonnances.Any(e => e.Id == id);
    }

    private int GetCurrentDoctorId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == userId);
            return doctor?.Id ?? 0;
        }
        return 0;
    }

    [HttpGet("my")]
    [Authorize(Roles = "Patient,Doctor")]
    public async Task<ActionResult<IEnumerable<OrdonnanceDto>>> GetMyOrdonnances()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return Unauthorized();

        var ordonnances = await _context.Ordonnances
            .Include(o => o.Doctor)
            .Where(o => o.PatientId == patient.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrdonnanceDto
            {
                Id = o.Id,
                Title = string.IsNullOrEmpty(o.Diagnosis) ? "Ordonnance" : o.Diagnosis,
                Date = o.CreatedAt.ToString("dd MMM yyyy"),
                DoctorName = o.Doctor != null ? $"Dr. {o.Doctor.FirstName} {o.Doctor.LastName}" : "Médecin",
                PdfUrl = $"{Request.Scheme}://{Request.Host}/api/ordonnances/{o.Id}/pdf?format=digital"
            })
            .ToListAsync();

        return Ok(ordonnances);
    }

    [HttpGet("{id}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadPdf(int id, [FromQuery] string format = "digital")
    {
        bool isDigital = format != "print";

        var ordonnance = await _context.Ordonnances
            .Include(o => o.Doctor)
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (ordonnance == null) return NotFound();

        QuestPDF.Settings.License = LicenseType.Community;

        // Générer le QR Code de vérification uniquement pour la version numérique
        byte[] qrCodeImage = new byte[0];
        if (isDigital)
        {
            try
            {
                string qrContent = $"SympNet Verified\nOrdonnance: {ordonnance.OrdonnanceCode}\nDate: {ordonnance.CreatedAt:dd/MM/yyyy}\nPatient: {ordonnance.Patient?.FirstName} {ordonnance.Patient?.LastName}\nDocteur: Dr. {ordonnance.Doctor?.LastName}";
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                qrCodeImage = qrCode.GetGraphic(20);
            }
            catch { }
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);

                void ComposeHeader(IContainer container)
                {
                    container.Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text($"Dr. {ordonnance.Doctor?.FirstName} {ordonnance.Doctor?.LastName}").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                            column.Item().Text(ordonnance.Doctor?.Speciality ?? "Médecin").FontSize(14).FontColor(Colors.Grey.Medium);
                            column.Item().Text(ordonnance.Doctor?.Address ?? "Adresse du cabinet");
                            column.Item().Text("Contact : Cabinet Médical");
                        });

                        row.ConstantItem(150).AlignRight().Column(col => 
                        {
                            col.Item().AlignRight().Text($"Date : {ordonnance.CreatedAt:dd/MM/yyyy}").FontSize(12);
                            if (qrCodeImage.Length > 0)
                            {
                                col.Item().PaddingTop(10).AlignRight().Width(60).Height(60).Image(qrCodeImage);
                            }
                        });
                    });
                }

                void ComposeContent(IContainer container)
                {
                    container.PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Spacing(20);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Patient : ").SemiBold();
                                text.Span($"{ordonnance.Patient?.FirstName} {ordonnance.Patient?.LastName}");
                            });
                            row.RelativeItem().AlignRight().Text($"Code : {ordonnance.OrdonnanceCode}").FontSize(10).FontColor(Colors.Grey.Medium);
                        });

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        if (!string.IsNullOrEmpty(ordonnance.Diagnosis))
                        {
                            column.Item().Text(text =>
                            {
                                text.Span("Diagnostic : ").SemiBold();
                                text.Span(ordonnance.Diagnosis);
                            });
                        }

                        column.Item().PaddingTop(10).Text("PRESCRIPTIONS").FontSize(16).SemiBold().Underline();

                        try
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var meds = JsonSerializer.Deserialize<List<MedicationItem>>(ordonnance.MedicationsJson, options);
                            if (meds != null && meds.Count > 0)
                            {
                                foreach (var med in meds)
                                {
                                    var medName = string.IsNullOrEmpty(med.Name) ? "" : med.Name;
                                    var dosage = string.IsNullOrEmpty(med.Dosage) ? "" : med.Dosage;
                                    var duration = string.IsNullOrEmpty(med.Duration) ? "" : med.Duration;
                                    var frequency = string.IsNullOrEmpty(med.Frequency) ? "" : med.Frequency;
                                    
                                    column.Item().PaddingLeft(10).Text($"• {medName} - {dosage}");
                                    if (!string.IsNullOrEmpty(duration) || !string.IsNullOrEmpty(frequency))
                                    {
                                        column.Item().PaddingLeft(25).Text($"{frequency} | {duration}").FontColor(Colors.Grey.Medium).FontSize(10);
                                    }
                                }
                            }
                            else
                            {
                                column.Item().Text("Aucun médicament spécifique.").FontColor(Colors.Grey.Medium);
                            }
                        }
                        catch
                        {
                            column.Item().Text(ordonnance.MedicationsJson);
                        }

                        if (!string.IsNullOrEmpty(ordonnance.Notes))
                        {
                            column.Item().PaddingTop(10).Text(text =>
                            {
                                text.Span("Notes / Conseils : ").SemiBold();
                                text.Span(ordonnance.Notes);
                            });
                        }

                        // Ajout du bloc des signatures (Électronique ou Physique selon le format)
                        column.Item().PaddingTop(50).Row(r =>
                        {
                            if (isDigital)
                            {
                                // Mentions de certification numérique (pour le patient)
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Ordonnance certifiée électroniquement via SympNet").FontSize(10).FontColor(Colors.Grey.Darken2).Italic().SemiBold();
                                    c.Item().Text($"ID Unique : {ordonnance.OrdonnanceCode}").FontSize(10).FontColor(Colors.Grey.Darken1);
                                    c.Item().PaddingTop(8).Text($"Signé numériquement par :").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text($"Dr. {ordonnance.Doctor?.FirstName} {ordonnance.Doctor?.LastName}").FontSize(11).SemiBold().FontColor(Colors.Blue.Darken2);
                                });
                            }
                            else
                            {
                                // Espace de cachet physique (pour le médecin)
                                r.RelativeItem().AlignRight().Column(c =>
                                {
                                    c.Item().AlignRight().Text("Signature et Cachet du Médecin").SemiBold().FontSize(10);
                                    c.Item().PaddingTop(30).AlignRight().Text("______________________________").FontColor(Colors.Grey.Lighten1);
                                });
                            }
                        });
                    });
                }

                void ComposeFooter(IContainer container)
                {
                    container.AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                }
            });
        });

        var pdfStream = new MemoryStream();
        document.GeneratePdf(pdfStream);
        pdfStream.Position = 0;

        return File(pdfStream, "application/pdf", $"Ordonnance_{ordonnance.OrdonnanceCode}.pdf");
    }
}

public class OrdonnanceCreateDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? ConsultationId { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string MedicationsJson { get; set; } = "[]";
    public string? Notes { get; set; }
    public bool HasAIAlerts { get; set; }
    public string? AIAlertsJson { get; set; }
}

public class OrdonnanceDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PdfUrl { get; set; } = string.Empty;
}

public class MedicationItem
{
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}
