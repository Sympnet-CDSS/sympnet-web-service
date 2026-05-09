using System.ComponentModel.DataAnnotations;

namespace SympNet.WebApi.Models;

public class QuickReply
{
    [Key]
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-comment";
    public int Order { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? DoctorSpeciality { get; set; }
}