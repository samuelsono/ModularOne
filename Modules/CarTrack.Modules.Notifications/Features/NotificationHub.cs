using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CarTrack.Modules.Notifications;

[Authorize]
public class NotificationHub : Hub
{
    public override Task OnConnectedAsync() => Task.CompletedTask;
}
