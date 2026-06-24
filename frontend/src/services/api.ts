import axios from 'axios';
import type { Fund } from '../types/fund';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

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

  refreshMarketData: async (): Promise<{ fundsUpdated: number }> => {
    const { data } = await api.post('/funds/refresh');
    return data;
  },
};

export default api;
