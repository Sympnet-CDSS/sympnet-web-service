using System.ComponentModel.DataAnnotations;

namespace SympNet.WebApi.Models;

public class ContactMessage
{
    public int Id { get; set; }
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? Phone { get; set; }
    
    [Required]
    public string Topic { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
