using System;

namespace SympNet.WebApi.Models;

public class CallSession
{
    public Guid CallId { get; set; }
    public Guid CallerId { get; set; }
    public string CallerName { get; set; } = string.Empty;
    public string CallerRole { get; set; } = string.Empty;
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverRole { get; set; } = string.Empty;
    public string CallType { get; set; } = "video";
    public string Status { get; set; } = "waiting"; // waiting, connected, ended, rejected
    public DateTime StartedAt { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int Duration { get; set; } = 0;
}