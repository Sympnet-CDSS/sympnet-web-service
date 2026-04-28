// SympNet.WebApi/DTOs/LoginResponseDto.cs
//
// This is what your login endpoint must return.
// The Android User.java model maps exactly to this JSON shape.
//
// Example JSON returned to Android:
// {
//   "id": "3fa85f64-...",
//   "email": "patient@example.com",
//   "fullName": null,           ← nullable, from User table
//   "role": "Patient",
//   "isActive": true,
//   "photoUrl": null,
//   "speciality": null,
//   "token": "eyJhbGci...",
//   "patient": {                ← nested Patient table data
//     "id": 1,
//     "firstName": "Ahmed",
//     "lastName": "Ben Ali",
//     "phoneNumber": "+216 XX XXX XXX",
//     "dateOfBirth": "1990-05-15T00:00:00",
//     "gender": "Male",
//     "address": "Tunis",
//     "bloodType": "A+",
//     "allergies": "None",
//     "medicalHistory": "",
//     "consultationCount": 3,
//     "userId": "3fa85f64-..."
//   }
// }

namespace SympNet.WebApi.DTOs;

public class LoginResponseDto
{
    // ── From User table ──────────────────────────────────────────────────
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }        // User.FullName (nullable)
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Speciality { get; set; }        // Doctors only
    public string Token { get; set; } = string.Empty;   // JWT

    // ── From Patient table (null if role != "Patient") ───────────────────
    public PatientDto? Patient { get; set; }
}

public class PatientDto
{
    // Mirrors C# Patient entity
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string Allergies { get; set; } = string.Empty;
    public string MedicalHistory { get; set; } = string.Empty;
    public int ConsultationCount { get; set; }
    public Guid UserId { get; set; }
}