namespace SympNet.WebApi.Dtos;

public class WorkingHoursDto
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDuration { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWorkingHoursDto
{
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDuration { get; set; } = 30;
}

public class UpdateWorkingHoursDto
{
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? SlotDuration { get; set; }
    public bool? IsActive { get; set; }
}

public class BlockedSlotDto
{
    public int Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
}

public class CreateBlockedSlotDto
{
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsRecurring { get; set; } = false;
    public string? RecurrencePattern { get; set; }
}

public class AgendaEventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? PatientEmail { get; set; }
    public string? Reason { get; set; }
    public bool IsUrgent { get; set; }
}

public class AvailableSlotResponseDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
}