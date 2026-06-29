import axios from 'axios';
import type { Fund } from '../types/fund';
import type { FundFilters } from '../store/filterStore';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

export interface FilteredFundsResponse {
  items: Fund[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const fundsApi = {
  getTopFunds: async (count = 100): Promise<Fund[]> => {
    const { data } = await api.get(`/funds/top/${count}`);
    return data;
  },

  getFundDetail: async (id: string): Promise<Fund> => {
    const { data } = await api.get(`/funds/${id}`);
    return data;
  },

  getFundsByRisk: async (riskLevel: number): Promise<Fund[]> => {
    const { data } = await api.get(`/funds/by-risk/${riskLevel}`);
    return data;
  },

  getFilteredFunds: async (filters: FundFilters): Promise<FilteredFundsResponse> => {
    const params: Record<string, string | number> = {};
    if (filters.riskLevel !== null) params.riskLevel = filters.riskLevel;
    if (filters.category) params.category = filters.category;
    if (filters.managementCompany) params.managementCompany = filters.managementCompany;
    if (filters.minRating !== null) params.minRating = filters.minRating;
    if (filters.maxExpenseRatio !== null) params.maxExpenseRatio = filters.maxExpenseRatio;
    if (filters.minReturn1Year !== null) params.minReturn1Year = filters.minReturn1Year;
    if (filters.maxVolatility !== null) params.maxVolatility = filters.maxVolatility;
    if (filters.search) params.search = filters.search;
    params.sortBy = filters.sortBy;
    if (filters.sortDesc) params.sortDesc = 'true';
    params.page = filters.page;
    params.pageSize = filters.pageSize;

    const { data } = await api.get('/funds/filter', { params });
    return data;
  },

  getCategories: async (): Promise<string[]> => {
    const { data } = await api.get('/funds/categories');
    return data;
  },

  getManagementCompanies: async (): Promise<string[]> => {
    const { data } = await api.get('/funds/management-companies');
    return data;
  },

  refreshMarketData: async (): Promise<{ fundsUpdated: number }> => {
    const { data } = await api.post('/funds/refresh');
    return data;
  },
};

export default api;
