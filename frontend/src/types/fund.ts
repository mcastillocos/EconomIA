export interface Fund {
  id: string;
  isin: string;
  name: string;
  category: string;
  managementCompany: string;
  riskLevel: RiskLevel;
  netAssetValue: number;
  currency: string;
  expenseRatio: number;
  rating: FundRating;
  rankingPosition: number;
  lastUpdated: string;
  latestPerformance: FundPerformance | null;
}

export interface FundPerformance {
  return1Month: number;
  return3Months: number;
  return6Months: number;
  return1Year: number;
  return3Years: number;
  return5Years: number;
  volatility: number;
  sharpeRatio: number;
  recordedAt: string;
}

export enum RiskLevel {
  VeryLow = 1,
  Low = 2,
  MediumLow = 3,
  Medium = 4,
  MediumHigh = 5,
  High = 6,
  VeryHigh = 7,
}

export enum FundRating {
  Unrated = 0,
  OneStar = 1,
  TwoStars = 2,
  ThreeStars = 3,
  FourStars = 4,
  FiveStars = 5,
}

export interface PriceUpdate {
  fundId: string;
  fundName: string;
  price: number;
  currency: string;
  timestamp: string;
}

export interface RankingChange {
  fundId: string;
  fundName: string;
  oldPosition: number;
  newPosition: number;
  timestamp: string;
}
