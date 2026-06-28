import axios from 'axios';
import type { Watchlist, CreateWatchlistRequest, AddWatchlistItemRequest } from '../types/watchlist';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

export const watchlistsApi = {
  getAll: async (): Promise<Watchlist[]> => {
    const { data } = await api.get('/watchlists');
    return data;
  },

  getById: async (id: string): Promise<Watchlist> => {
    const { data } = await api.get(`/watchlists/${id}`);
    return data;
  },

  create: async (request: CreateWatchlistRequest): Promise<Watchlist> => {
    const { data } = await api.post('/watchlists', request);
    return data;
  },

  update: async (id: string, request: CreateWatchlistRequest): Promise<Watchlist> => {
    const { data } = await api.put(`/watchlists/${id}`, request);
    return data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/watchlists/${id}`);
  },

  addItem: async (watchlistId: string, request: AddWatchlistItemRequest): Promise<void> => {
    await api.post(`/watchlists/${watchlistId}/items`, request);
  },

  removeItem: async (watchlistId: string, itemId: string): Promise<void> => {
    await api.delete(`/watchlists/${watchlistId}/items/${itemId}`);
  },
};
