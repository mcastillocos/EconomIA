export interface FinancialMetric {
  id: string;
  entityType: string;
  entityId?: string;
  ticker?: string;
  isin?: string;
  metricName: string;
  value: number;
  period?: string;
  year?: number;
  quarter?: number;
  currency?: string;
  source?: string;
  sourceType?: string;
  fileName?: string;
  page?: string;
  row?: string;
  url?: string;
  confidence: 'high' | 'medium' | 'low';
  rawText?: string;
  validated: boolean;
  validatedAt?: string;
  createdAt: string;
}

export interface MetricFilter {
  entityType?: string;
  entityId?: string;
  metricName?: string;
  year?: number;
  quarter?: number;
  source?: string;
  validated?: boolean;
}
