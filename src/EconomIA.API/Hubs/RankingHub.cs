using Microsoft.AspNetCore.SignalR;

namespace EconomIA.API.Hubs;

public class RankingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "ranking-watchers");
        await base.OnConnectedAsync();
    }
}
