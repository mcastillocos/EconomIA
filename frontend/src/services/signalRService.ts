import * as signalR from '@microsoft/signalr';

class SignalRService {
  private priceConnection: signalR.HubConnection | null = null;
  private rankingConnection: signalR.HubConnection | null = null;
  private presenceConnection: signalR.HubConnection | null = null;

  async connectPriceHub(): Promise<signalR.HubConnection> {
    if (this.priceConnection?.state === signalR.HubConnectionState.Connected) {
      return this.priceConnection;
    }

    this.priceConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/fund-prices')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    await this.priceConnection.start();
    console.log('Connected to FundPrice Hub');
    return this.priceConnection;
  }

  async connectRankingHub(): Promise<signalR.HubConnection> {
    if (this.rankingConnection?.state === signalR.HubConnectionState.Connected) {
      return this.rankingConnection;
    }

    this.rankingConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/ranking')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    await this.rankingConnection.start();
    console.log('Connected to Ranking Hub');
    return this.rankingConnection;
  }

  async connectPresenceHub(): Promise<signalR.HubConnection> {
    if (this.presenceConnection?.state === signalR.HubConnectionState.Connected) {
      return this.presenceConnection;
    }

    this.presenceConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/presence')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    await this.presenceConnection.start();
    console.log('Connected to Presence Hub');
    return this.presenceConnection;
  }

  async disconnect(): Promise<void> {
    await this.priceConnection?.stop();
    await this.rankingConnection?.stop();
    await this.presenceConnection?.stop();
  }
}

export const signalRService = new SignalRService();
