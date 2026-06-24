import { useQuery } from '@tanstack/react-query';
import { fundsApi } from '../services/api';
import type { Fund } from '../types/fund';

export function useTopFunds(count = 100) {
  return useQuery<Fund[]>({
    queryKey: ['topFunds', count],
    queryFn: () => fundsApi.getTopFunds(count),
    refetchInterval: 60_000,
  });
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
