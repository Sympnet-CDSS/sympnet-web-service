using System.ComponentModel.DataAnnotations;

namespace SympNet.WebApi.Models;

public class BlogPost
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";
    
    [Required]
    public string Content { get; set; } = "";
    
    [MaxLength(500)]
    public string Summary { get; set; } = "";
    
    public string ImageUrl { get; set; } = "";
    
    [MaxLength(50)]
    public string Category { get; set; } = "Général"; // e.g., IA, Santé, Tech, Conseil
    
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int ReadingTime { get; set; } = 5; // in minutes
    
    public bool IsPublished { get; set; } = true;
    
    public int Views { get; set; } = 0;
}
