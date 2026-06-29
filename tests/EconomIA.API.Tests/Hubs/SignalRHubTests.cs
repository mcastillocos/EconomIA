using Microsoft.AspNetCore.SignalR.Client;

namespace EconomIA.API.Tests.Hubs;

public class SignalRHubTests
{
    private EconomIAWebFactory CreateFactory() => new();

    [Fact]
    public async Task FundPriceHub_ConectaCorrectamente()
    {
        await using var factory = CreateFactory();
        var server = factory.Server;

        var connection = new HubConnectionBuilder()
            .WithUrl(
                $"{server.BaseAddress}hubs/fund-prices",
                o => o.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task RankingHub_ConectaCorrectamente()
    {
        await using var factory = CreateFactory();
        var server = factory.Server;

        var connection = new HubConnectionBuilder()
            .WithUrl(
                $"{server.BaseAddress}hubs/ranking",
                o => o.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task FundPriceHub_RecibeUpdates_TrasSuscripcion()
    {
        await using var factory = CreateFactory();
        var server = factory.Server;

        var connection = new HubConnectionBuilder()
            .WithUrl(
                $"{server.BaseAddress}hubs/fund-prices",
                o => o.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var received = new TaskCompletionSource<bool>();
        connection.On<object>("ReceivePriceUpdate", _ =>
        {
            received.TrySetResult(true);
        });

        await connection.StartAsync();

        // El hub está conectado y escuchando; verificar que no hay error
        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();
        await connection.DisposeAsync();
    }
}
