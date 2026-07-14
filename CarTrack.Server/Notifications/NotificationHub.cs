using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CarTrack.Server.Notifications;

[Authorize]
public class NotificationHub : Hub
{
    public override Task OnConnectedAsync() => Task.CompletedTask;
}
