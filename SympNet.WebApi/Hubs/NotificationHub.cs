using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SympNet.WebApi.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                  ?? Context.User?.FindFirst("sub")?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            if (role != null && role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"doctor_user_{userId}");
            }
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinDoctorGroup(string doctorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"doctor_{doctorId}");
    }

    public async Task MarkNotificationRead(int notificationId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Clients.Caller.SendAsync("NotificationMarkedRead", notificationId);
    }
}
