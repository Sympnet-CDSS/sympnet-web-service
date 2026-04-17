namespace SympNet.WebApi.Dtos;

public class ProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateEmailDto
{
    public string Email { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdatePhotoDto
{
    public string PhotoBase64 { get; set; } = string.Empty;
}