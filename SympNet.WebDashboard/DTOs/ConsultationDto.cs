namespace SympNet.WebDashboard.DTOs
{
    public class ConsultationDto
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = "";
        public string PatientEmail { get; set; } = "";
        public string Diagnosis { get; set; } = "";
        public double ConfidenceScore { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
