import { create } from 'zustand';
import type { RiskLevel, FundRating } from '../types/fund';

export type FundSortBy =
  | 'Ranking'
  | 'Name'
  | 'Return1Year'
  | 'ExpenseRatio'
  | 'Rating'
  | 'Volatility'
  | 'NetAssetValue';

export interface FundFilters {
  riskLevel: RiskLevel | null;
  category: string | null;
  managementCompany: string | null;
  minRating: FundRating | null;
  maxExpenseRatio: number | null;
  minReturn1Year: number | null;
  maxVolatility: number | null;
  search: string | null;
  sortBy: FundSortBy;
  sortDesc: boolean;
  page: number;
  pageSize: number;
}

interface FilterStore {
  filters: FundFilters;
  categories: string[];
  managementCompanies: string[];
  setFilter: <K extends keyof FundFilters>(key: K, value: FundFilters[K]) => void;
  resetFilters: () => void;
  setCategories: (cats: string[]) => void;
  setManagementCompanies: (companies: string[]) => void;
  setPage: (page: number) => void;
  hasActiveFilters: () => boolean;
}

const defaultFilters: FundFilters = {
  riskLevel: null,
  category: null,
  managementCompany: null,
  minRating: null,
  maxExpenseRatio: null,
  minReturn1Year: null,
  maxVolatility: null,
  search: null,
  sortBy: 'Ranking',
  sortDesc: false,
  page: 1,
  pageSize: 50,
};

export const useFilterStore = create<FilterStore>((set, get) => ({
  filters: { ...defaultFilters },
  categories: [],
  managementCompanies: [],

  setFilter: (key, value) =>
    set((state) => ({
      filters: { ...state.filters, [key]: value, page: key === 'page' ? (value as number) : 1 },
    })),

  resetFilters: () => set({ filters: { ...defaultFilters } }),

  setCategories: (cats) => set({ categories: cats }),
  setManagementCompanies: (companies) => set({ managementCompanies: companies }),

  setPage: (page) =>
    set((state) => ({ filters: { ...state.filters, page } })),

  hasActiveFilters: () => {
    const f = get().filters;
    return (
      f.riskLevel !== null ||
      f.category !== null ||
      f.managementCompany !== null ||
      f.minRating !== null ||
      f.maxExpenseRatio !== null ||
      f.minReturn1Year !== null ||
      f.maxVolatility !== null ||
      (f.search !== null && f.search.length > 0)
    );
  },
}));
