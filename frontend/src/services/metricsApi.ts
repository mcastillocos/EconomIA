import axios from 'axios';
import type { FinancialMetric, MetricFilter } from '../types/metric';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

export const metricsApi = {
  getFiltered: async (filter: MetricFilter = {}): Promise<FinancialMetric[]> => {
    const params = new URLSearchParams();
    if (filter.entityType) params.append('entityType', filter.entityType);
    if (filter.entityId) params.append('entityId', filter.entityId);
    if (filter.metricName) params.append('metricName', filter.metricName);
    if (filter.year) params.append('year', filter.year.toString());
    if (filter.quarter) params.append('quarter', filter.quarter.toString());
    if (filter.source) params.append('source', filter.source);
    if (filter.validated !== undefined) params.append('validated', filter.validated.toString());

    const { data } = await api.get(`/metrics?${params.toString()}`);
    return data;
  },

  validate: async (id: string): Promise<void> => {
    await api.post(`/metrics/${id}/validate`);
  },

  unvalidate: async (id: string): Promise<void> => {
    await api.post(`/metrics/${id}/unvalidate`);
  },
};
