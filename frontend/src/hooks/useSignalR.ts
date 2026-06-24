import { useEffect, useRef, useCallback } from 'react';
import { signalRService } from '../services/signalRService';
import type { PriceUpdate, RankingChange } from '../types/fund';
import { useFundStore } from '../store/fundStore';

export function useSignalR() {
  const isConnected = useRef(false);
  const updateFundPrice = useFundStore((s) => s.updateFundPrice);
  const setRefreshNeeded = useFundStore((s) => s.setRefreshNeeded);

  const connect = useCallback(async () => {
    if (isConnected.current) return;

    try {
      const priceConn = await signalRService.connectPriceHub();
      const rankingConn = await signalRService.connectRankingHub();

      priceConn.on('PriceUpdated', (update: PriceUpdate) => {
        updateFundPrice(update.fundId, update.price, update.currency);
      });

      rankingConn.on('TopFundsRefreshed', () => {
        setRefreshNeeded(true);
      });

      rankingConn.on('RankingChanged', (_change: RankingChange) => {
        setRefreshNeeded(true);
      });

      isConnected.current = true;
    } catch (err) {
      console.error('SignalR connection failed:', err);
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
