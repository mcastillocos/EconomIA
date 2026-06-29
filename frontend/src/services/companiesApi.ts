import axios from 'axios';
import type { Company, CreateCompanyRequest, UpdateCompanyRequest } from '../types/company';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

export const companiesApi = {
  getAll: async (): Promise<Company[]> => {
    const { data } = await api.get('/companies');
    return data;
  },

  getById: async (id: string): Promise<Company> => {
    const { data } = await api.get(`/companies/${id}`);
    return data;
  },

  create: async (request: CreateCompanyRequest): Promise<Company> => {
    const { data } = await api.post('/companies', request);
    return data;
  },

  update: async (id: string, request: UpdateCompanyRequest): Promise<Company> => {
    const { data } = await api.put(`/companies/${id}`, request);
    return data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/companies/${id}`);
  },

  aiLookup: async (query: string): Promise<CreateCompanyRequest> => {
    const { data } = await api.post('/companies/ai-lookup', { query });
    return data;
  },
};
