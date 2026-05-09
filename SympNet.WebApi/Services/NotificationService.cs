using Microsoft.AspNetCore.SignalR;
using SympNet.WebApi.Hubs;

namespace SympNet.WebApi.Services;

public interface INotificationService
{
    Task SendNewMessageNotification(Guid userId, string message);
    Task SendCallNotification(Guid userId, string callerName);
    Task SendTypingNotification(Guid userId, string userName);
}

public class NotificationService : INotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<ChatHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNewMessageNotification(Guid userId, string message)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("NewMessageNotification", new
        {
            message = message.Length > 50 ? message[..50] + "..." : message,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendCallNotification(Guid userId, string callerName)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("IncomingCallNotification", new
        {
            callerName,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendTypingNotification(Guid userId, string userName)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("UserTypingNotification", new
        {
            userName,
            isTyping = true
        });
    }
}