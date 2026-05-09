namespace SympNet.WebApi.Dtos;

public class WorkingHourDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
}

public class UpsertWorkingHoursDto
{
    public List<WorkingHourDto> Hours { get; set; } = new();
}

public class TimeSlotDto
{
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public bool IsAvailable { get; set; }
    
    public TimeSlotDto(TimeOnly start, TimeOnly end, bool isAvailable)
    {
        Start = start;
        End = end;
        IsAvailable = isAvailable;
    }
}

public class DoctorAvailabilityDto
{
    public int DoctorId { get; set; }
    public DateTime Date { get; set; }
    public List<TimeSlotDto> AvailableSlots { get; set; }
    
    public DoctorAvailabilityDto(int doctorId, DateTime date, List<TimeSlotDto> slots)
    {
        DoctorId = doctorId;
        Date = date;
        AvailableSlots = slots;
    }
}