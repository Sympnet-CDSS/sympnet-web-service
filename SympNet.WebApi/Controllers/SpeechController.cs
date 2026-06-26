using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpeechController : ControllerBase
{
    [HttpPost("transcribe")]
    public async Task<IActionResult> TranscribeAudio([FromBody] AudioRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Audio))
                return BadRequest(new { error = "Aucune donnée audio reçue" });

            // Décoder le base64
            byte[] audioBytes = Convert.FromBase64String(request.Audio);
            
            var result = await SimulateTranscription(audioBytes);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<TranscriptionResult> SimulateTranscription(byte[] audioBytes)
    {
        await Task.Delay(800); 
        
        var mockTexts = new[]
        {
            "Bonjour docteur, j'ai une douleur à la poitrine depuis hier.",
            "Je tousse beaucoup et j'ai de la fièvre depuis trois jours.",
            "Mon enfant a mal à la gorge et ne veut pas manger.",
            "J'aimerais prendre un rendez-vous pour une consultation.",
            "J'ai besoin d'un renouvellement d'ordonnance s'il vous plaît.",
            "Je me sens fatigué et j'ai des maux de tête fréquents.",
            "Ma tension artérielle est élevée ces derniers temps.",
            "J'ai une douleur au genou qui me gêne pour marcher."
        };
        
        var random = new Random(audioBytes.Length);
        var selectedText = mockTexts[random.Next(mockTexts.Length)];
        
        return new TranscriptionResult
        {
            Text = selectedText,
            Success = true,
            Confidence = 0.95
        };
    }
}

public class AudioRequest
{
    public string Audio { get; set; } = "";
}

public class TranscriptionResult
{
    public string Text { get; set; } = "";
    public bool Success { get; set; }
    public double Confidence { get; set; }
    public string? Error { get; set; }
}