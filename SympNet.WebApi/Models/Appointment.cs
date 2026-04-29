using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SympNet.WebApi.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }
        
        public Guid PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime DateTime { get; set; }
        public int Duration { get; set; } = 30;
        public string Status { get; set; } = "En attente";
        public string? Type { get; set; } = "Consultation";
        public string? Notes { get; set; }
        public string? Reason { get; set; }
        public bool IsUrgent { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ReminderSentAt { get; set; }
        
        [ForeignKey("PatientId")]
        public virtual User? Patient { get; set; }
        
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
    }
}