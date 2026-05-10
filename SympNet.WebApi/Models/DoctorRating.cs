namespace SympNet.WebApi.Models;

public class DoctorRating
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int Score { get; set; }  // 1-5
    public string? Comment { get; set; }
    public bool IsModerated { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
