using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameOrganizer.Api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
