using Microsoft.AspNetCore.SignalR;

namespace EconomIA.API.Hubs;

public class PresenceHub : Hub
{
    private static int _connectedUsers;

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectedUsers);
        await Clients.All.SendAsync("UserCountChanged", _connectedUsers);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _connectedUsers);
        await Clients.All.SendAsync("UserCountChanged", _connectedUsers);
        await base.OnDisconnectedAsync(exception);
    }

    public static int ConnectedUsers => _connectedUsers;
}
