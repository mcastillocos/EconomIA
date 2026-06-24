import * as signalR from '@microsoft/signalr';

class SignalRService {
  private priceConnection: signalR.HubConnection | null = null;
  private rankingConnection: signalR.HubConnection | null = null;

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

  async disconnect(): Promise<void> {
    await this.priceConnection?.stop();
    await this.rankingConnection?.stop();
  }
}

export const signalRService = new SignalRService();
