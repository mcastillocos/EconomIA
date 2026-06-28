export interface Company {
  id: string;
  name: string;
  ticker?: string;
  isin?: string;
  market?: string;
  country?: string;
  sector?: string;
  industry?: string;
  currency?: string;
  competitors?: string;
  relevantUrls?: string;
  preferredSource?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCompanyRequest {
  name: string;
  ticker?: string;
  isin?: string;
  market?: string;
  country?: string;
  sector?: string;
  industry?: string;
  currency?: string;
  competitors?: string;
  relevantUrls?: string;
  preferredSource?: string;
  notes?: string;
}

export type UpdateCompanyRequest = CreateCompanyRequest;
