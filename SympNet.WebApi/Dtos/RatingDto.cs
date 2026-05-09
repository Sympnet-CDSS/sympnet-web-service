namespace SympNet.WebApi.Dtos;

public class RatingDto
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRatingDto
{
    public int DoctorId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
}