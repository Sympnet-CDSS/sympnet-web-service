using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SympNet.WebApi.Models;

public class Message
{
    [Key]
    public int Id { get; set; }
    
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public string? AttachmentName { get; set; }
    public long? AttachmentSize { get; set; }
    
    public bool IsRead { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public bool IsDeletedForSender { get; set; } = false;
    public bool IsDeletedForReceiver { get; set; } = false;
    
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    [ForeignKey("SenderId")]
    public virtual User? Sender { get; set; }
    
    [ForeignKey("ReceiverId")]
    public virtual User? Receiver { get; set; }
}