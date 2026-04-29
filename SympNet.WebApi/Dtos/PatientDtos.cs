using System;
using System.ComponentModel.DataAnnotations;

namespace SympNet.WebApi.Dtos
{
    public class PatientDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public int ConsultationCount { get; set; }
    }

    public class CreatePatientDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public DateTime DateOfBirth { get; set; } = DateTime.UtcNow.AddYears(-30);
        
        public string Gender { get; set; } = string.Empty;
    }
}