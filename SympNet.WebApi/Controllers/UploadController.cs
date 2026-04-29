using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpPost("file")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string type = "general")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
        
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Type de fichier non autorisé" });

        if (file.Length > 10 * 1024 * 1024) // 10 MB
            return BadRequest(new { message = "Fichier trop volumineux (max 10 MB)" });

        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", type);
        if (!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{type}/{fileName}";

        return Ok(new
        {
            url = fileUrl,
            fileName = file.FileName,
            size = file.Length,
            type = file.ContentType
        });
    }

    [HttpGet("download/{type}/{fileName}")]
    public IActionResult DownloadFile(string type, string fileName)
    {
        var filePath = Path.Combine(_env.WebRootPath, "uploads", type, fileName);
        
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var memory = new MemoryStream();
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            stream.CopyTo(memory);
        }
        memory.Position = 0;
        
        var contentType = "application/octet-stream";
        return File(memory, contentType, fileName);
    }
}