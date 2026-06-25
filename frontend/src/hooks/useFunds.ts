import { useQuery } from '@tanstack/react-query';
import { useEffect, useRef, useCallback, useState } from 'react';
import { fundsApi } from '../services/api';
import type { Fund } from '../types/fund';
import { appLog } from '../store/logStore';
import { useFundStore } from '../store/fundStore';

// ── SSE Streaming hook (carga progresiva con workers paralelos) ────────

interface StreamState {
  funds: Fund[];
  isStreaming: boolean;
  workersCompleted: number;
  workersTotal: number;
  error: string | null;
}

export function useStreamFunds() {
  const [state, setState] = useState<StreamState>({
    funds: [],
    isStreaming: false,
    workersCompleted: 0,
    workersTotal: 10,
    error: null,
  });
  const eventSourceRef = useRef<EventSource | null>(null);
  const setFunds = useFundStore((s) => s.setFunds);

  const startStream = useCallback(() => {
    // Close previous
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
    }

    setState({ funds: [], isStreaming: true, workersCompleted: 0, workersTotal: 10, error: null });
    appLog.info('LLM', 'Iniciando carga paralela con SSE (10 workers × 10 fondos)...');

    const es = new EventSource('/api/llm/funds/stream');
    eventSourceRef.current = es;

    es.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data);

        if (data.type === 'status') {
          appLog.info('LLM', data.message);
        } else if (data.type === 'batch') {
          setState((prev) => {
            const merged = [...prev.funds, ...data.newFunds];
            setFunds(merged as Fund[]);
            appLog.success('LLM', `Worker ${data.workerIndex + 1}: +${data.newFunds.length} fondos (total: ${data.totalSoFar})`);
            return {
              ...prev,
              funds: merged as Fund[],
              workersCompleted: data.workersCompleted,
              workersTotal: data.workersTotal,
            };
          });
        } else if (data.type === 'error') {
          appLog.error('LLM', `Worker ${data.workerIndex + 1} falló: ${data.message}`);
          setState((prev) => ({
            ...prev,
            workersCompleted: data.workersCompleted,
            workersTotal: data.workersTotal,
          }));
        } else if (data.type === 'done') {
          appLog.success('LLM', `Carga completa: ${data.totalFunds} fondos en ${data.elapsed}s`);
          setState((prev) => ({ ...prev, isStreaming: false }));
          es.close();
        }
      } catch {
        // Ignore parse errors on individual events
      }
    };

    es.onerror = () => {
      appLog.warn('LLM', 'SSE no disponible, usando API REST como fallback...');
      es.close();
      // Fallback to REST API
      fundsApi.getTopFunds(100)
        .then((data) => {
          if (data.length > 0) {
            setFunds(data);
            appLog.success('API', `Fallback REST: ${data.length} fondos cargados`);
            setState((prev) => ({ ...prev, funds: data, isStreaming: false }));
          } else {
            setState((prev) => ({ ...prev, isStreaming: false, error: 'Sin datos disponibles' }));
          }
        })
        .catch(() => {
          appLog.error('API', 'Fallback REST también falló');
          setState((prev) => ({ ...prev, isStreaming: false, error: 'Error cargando datos' }));
        });
    };
  }, [setFunds]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      eventSourceRef.current?.close();
    };
  }, []);

  return { ...state, startStream };
}

// ── Classic fetch (fallback + react-query) ─────────────────────────────

async function fetchTopFunds(count: number, signal: AbortSignal): Promise<Fund[]> {
  // 1. LLM con workers paralelos
  try {
    appLog.info('LLM', 'Solicitando datos a workers paralelos...');
    const res = await fetch('/api/llm/funds', { signal });
    if (!res.ok) throw new Error(`LLM API ${res.status}`);
    const data = await res.json();
    if (Array.isArray(data) && data.length > 0) {
      appLog.success('LLM', `${data.length} fondos recibidos de workers paralelos`);
      return data;
    }
  } catch (err) {
    if ((err as Error).name === 'AbortError') throw err;
    appLog.warn('LLM', `Workers no disponibles: ${(err as Error).message}`);
  }

  // 2. Backend .NET fallback
  try {
    appLog.info('API', 'Intentando backend .NET...');
    const data = await fundsApi.getTopFunds(count);
    if (data.length > 0) {
      appLog.success('API', `Backend: ${data.length} fondos`);
      return data;
    }
  } catch {
    appLog.warn('API', 'Backend .NET no disponible');
  }

  appLog.error('App', 'No se pudieron obtener datos de ninguna fuente');
  return [];
}

export function useTopFunds(count = 100) {
  const query = useQuery<Fund[]>({
    queryKey: ['topFunds', count],
    queryFn: ({ signal }) => fetchTopFunds(count, signal),
    retry: 1,
    retryDelay: 3000,
    staleTime: 5 * 60 * 1000,
  });

  return query;
}

export function useFundDetail(id: string) {
  return useQuery<Fund>({
    queryKey: ['fund', id],
    queryFn: () => fundsApi.getFundDetail(id),
    enabled: !!id,
  });
}

export function useFundsByRisk(riskLevel: number) {
  return useQuery<Fund[]>({
    queryKey: ['fundsByRisk', riskLevel],
    queryFn: () => fundsApi.getFundsByRisk(riskLevel),
  });
}
