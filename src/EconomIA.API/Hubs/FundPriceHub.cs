using Microsoft.AspNetCore.SignalR;

namespace EconomIA.API.Hubs;

public class FundPriceHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "fund-watchers");
        await base.OnConnectedAsync();
    }

    public async Task SubscribeToFund(string fundId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"fund-{fundId}");
    }

    public async Task UnsubscribeFromFund(string fundId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"fund-{fundId}");
    }
}
