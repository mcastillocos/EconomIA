import { useEffect, useRef, useCallback } from 'react';
import { signalRService } from '../services/signalRService';
import type { PriceUpdate, RankingChange } from '../types/fund';
import { useFundStore } from '../store/fundStore';
import { usePresenceStore } from '../store/presenceStore';
import { appLog } from '../store/logStore';

export function useSignalR() {
  const isConnected = useRef(false);
  const updateFundPrice = useFundStore((s) => s.updateFundPrice);
  const setRefreshNeeded = useFundStore((s) => s.setRefreshNeeded);
  const setOnlineCount = usePresenceStore((s) => s.setOnlineCount);

  const connect = useCallback(async () => {
    if (isConnected.current) return;

    try {
      // Check if backend is reachable before attempting SignalR negotiation
      const probe = await fetch('/hubs/fund-prices/negotiate?negotiateVersion=1', { method: 'POST' }).catch(() => null);
      if (!probe || !probe.ok) {
        appLog.debug('SignalR', 'Backend no disponible — conexión pospuesta');
        return;
      }

      const priceConn = await signalRService.connectPriceHub();
      const rankingConn = await signalRService.connectRankingHub();

      priceConn.on('PriceUpdated', (update: PriceUpdate) => {
        updateFundPrice(update.fundId, update.price, update.currency);
        appLog.info('SignalR', `Precio actualizado: ${update.fundId} → ${update.price} ${update.currency}`);
      });

      rankingConn.on('TopFundsRefreshed', () => {
        setRefreshNeeded(true);
        appLog.info('SignalR', 'Top fondos refrescados — se requiere actualización');
      });

      rankingConn.on('RankingChanged', (_change: RankingChange) => {
        setRefreshNeeded(true);
        appLog.info('SignalR', 'Cambio en ranking detectado');
      });

      isConnected.current = true;
      appLog.success('SignalR', 'Conectado a hubs de precios y ranking');

      // Presence hub
      try {
        const presenceConn = await signalRService.connectPresenceHub();
        presenceConn.on('UserCountChanged', (count: number) => {
          setOnlineCount(count);
          appLog.debug('SignalR', `Usuarios conectados: ${count}`);
        });
        appLog.success('SignalR', 'Conectado a hub de presencia');
      } catch (err) {
        appLog.warn('SignalR', `Presencia no disponible: ${(err as Error).message}`);
      }
    } catch (err) {
      appLog.warn('SignalR', `Conexión fallida: ${(err as Error).message}`);
    }
  }, [updateFundPrice, setRefreshNeeded]);

  useEffect(() => {
    connect();
    return () => {
      signalRService.disconnect();
      isConnected.current = false;
    };
  }, [connect]);

  return { isConnected: isConnected.current };
}
