import { useQuery } from '@tanstack/react-query';
import { fundsApi, type FilteredFundsResponse } from '../services/api';
import { useFilterStore } from '../store/filterStore';

export function useFilteredFunds() {
  const filters = useFilterStore((s) => s.filters);
  const hasActiveFilters = useFilterStore((s) => s.hasActiveFilters());

  return useQuery<FilteredFundsResponse>({
    queryKey: ['filteredFunds', filters],
    queryFn: () => fundsApi.getFilteredFunds(filters),
    enabled: hasActiveFilters,
    staleTime: 30_000,
    placeholderData: (prev) => prev,
  });
}
