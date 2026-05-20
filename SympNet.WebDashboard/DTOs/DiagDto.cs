namespace SympNet.WebDashboard.DTOs
{
    public class DiagDto
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = "";
        public string Symptoms { get; set; } = "";
        public string Diagnosis { get; set; } = "";
        public double ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
