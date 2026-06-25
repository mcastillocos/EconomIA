import { describe, it, expect, beforeEach } from 'vitest';
import { useMyFundsStore, compareFundVsTop, type MyFund } from '../store/myFundsStore';
import type { Fund } from '../types/fund';

const makeFund = (overrides: Partial<MyFund> = {}): Omit<MyFund, 'id' | 'addedAt'> => ({
  isin: 'IE00B4L5Y983',
  name: 'iShares Core MSCI World',
  category: 'Renta Variable Global',
  managementCompany: 'BlackRock',
  riskLevel: 5,
  netAssetValue: 89.34,
  currency: 'EUR',
  expenseRatio: 0.2,
  rating: 5,
  latestPerformance: {
    return1Month: 2.1,
    return3Months: 5.4,
    return6Months: 8.7,
    return1Year: 18.5,
    return3Years: 42.3,
    return5Years: 78.1,
    volatility: 14.2,
    sharpeRatio: 1.3,
    recordedAt: new Date().toISOString(),
  },
  ...overrides,
});

const makeTopFund = (i: number): Fund => ({
  id: `f-${i}`,
  isin: `IE00B${i}`,
  name: `Fund ${i}`,
  category: 'Renta Variable Global',
  managementCompany: 'Test',
  riskLevel: 5 as any,
  netAssetValue: 100,
  currency: 'EUR',
  expenseRatio: 0.3,
  rating: 4 as any,
  rankingPosition: i,
  lastUpdated: new Date().toISOString(),
  latestPerformance: {
    return1Month: 1 + i * 0.5,
    return3Months: 3 + i,
    return6Months: 6 + i * 2,
    return1Year: 10 + i * 3,
    return3Years: 30 + i * 5,
    return5Years: 60 + i * 8,
    volatility: 10 + i,
    sharpeRatio: 0.8 + i * 0.1,
    recordedAt: new Date().toISOString(),
  },
});

describe('myFundsStore', () => {
  beforeEach(() => {
    useMyFundsStore.getState().clearAll();
  });

  it('should start empty', () => {
    expect(useMyFundsStore.getState().myFunds).toHaveLength(0);
  });

  it('should add a fund with generated id', () => {
    useMyFundsStore.getState().addFund(makeFund());
    const funds = useMyFundsStore.getState().myFunds;
    expect(funds).toHaveLength(1);
    expect(funds[0].isin).toBe('IE00B4L5Y983');
    expect(funds[0].id).toMatch(/^my-/);
    expect(funds[0].addedAt).toBeTruthy();
  });

  it('should remove a fund by id', () => {
    useMyFundsStore.getState().addFund(makeFund());
    const id = useMyFundsStore.getState().myFunds[0].id;
    useMyFundsStore.getState().removeFund(id);
    expect(useMyFundsStore.getState().myFunds).toHaveLength(0);
  });

  it('should clear all funds', () => {
    useMyFundsStore.getState().addFund(makeFund());
    useMyFundsStore.getState().addFund(makeFund({ isin: 'LU099' }));
    useMyFundsStore.getState().clearAll();
    expect(useMyFundsStore.getState().myFunds).toHaveLength(0);
  });
});

describe('compareFundVsTop', () => {
  const topFunds = Array.from({ length: 10 }, (_, i) => makeTopFund(i + 1));

  it('should return null if fund has no performance', () => {
    const fund = { ...makeFund(), latestPerformance: null, id: 'x', addedAt: '' } as MyFund;
    expect(compareFundVsTop(fund, topFunds)).toBeNull();
  });

  it('should return comparison with percentiles', () => {
    const fund = { ...makeFund(), id: 'x', addedAt: '' } as MyFund;
    const result = compareFundVsTop(fund, topFunds);
    expect(result).not.toBeNull();
    expect(result!.return1Year.mine).toBe(18.5);
    expect(result!.return1Year.topAvg).toBeGreaterThan(0);
    expect(result!.return1Year.topBest).toBeGreaterThan(0);
    expect(result!.return1Year.percentile).toBeGreaterThanOrEqual(0);
    expect(result!.return1Year.percentile).toBeLessThanOrEqual(100);
  });

  it('should compute all metric fields', () => {
    const fund = { ...makeFund(), id: 'x', addedAt: '' } as MyFund;
    const result = compareFundVsTop(fund, topFunds)!;
    expect(result).toHaveProperty('return1Year');
    expect(result).toHaveProperty('return3Years');
    expect(result).toHaveProperty('return5Years');
    expect(result).toHaveProperty('volatility');
    expect(result).toHaveProperty('sharpeRatio');
    expect(result).toHaveProperty('expenseRatio');
  });

  it('should use custom totalFunds for verdict labels', () => {
    const fund = { ...makeFund(), id: 'x', addedAt: '' } as MyFund;
    const result50 = compareFundVsTop(fund, topFunds, 50)!;
    expect(result50.verdict.headline).toContain('50');
    expect(result50.verdict.detail).toContain('50');

    const result200 = compareFundVsTop(fund, topFunds, 200)!;
    expect(result200.verdict.headline).toContain('200');
  });
});
