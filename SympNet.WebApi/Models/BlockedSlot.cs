using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SympNet.WebApi.Models
{
    public class BlockedSlot
    {
        [Key]
        public int Id { get; set; }
        
        public int DoctorId { get; set; }  
        
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
        
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsRecurring { get; set; } = false;
        public string? RecurrencePattern { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}