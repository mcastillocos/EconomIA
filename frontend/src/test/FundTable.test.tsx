import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import FundTable from '../components/Dashboard/FundTable';
import type { Fund } from '../types/fund';

const mockFund = (overrides: Partial<Fund> = {}): Fund => ({
  id: 'f-001',
  isin: 'IE00B4L5Y983',
  name: 'iShares Core MSCI World',
  category: 'Renta Variable Global',
  managementCompany: 'BlackRock',
  riskLevel: 5,
  netAssetValue: 89.34,
  currency: 'EUR',
  expenseRatio: 0.2,
  rating: 5,
  rankingPosition: 1,
  lastUpdated: new Date().toISOString(),
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

describe('FundTable', () => {
  it('should render ISIN column', () => {
    render(<FundTable funds={[mockFund()]} />);
    expect(screen.getByText('ISIN')).toBeInTheDocument();
    expect(screen.getByText('IE00B4L5Y983')).toBeInTheDocument();
  });

  it('should render ISIN filter input', () => {
    render(<FundTable funds={[mockFund()]} />);
    expect(screen.getByPlaceholderText(/IE00B4L5Y983/)).toBeInTheDocument();
  });

  it('should filter by ISIN', () => {
    const funds = [
      mockFund({ id: '1', isin: 'IE00B4L5Y983', name: 'Fund A', rankingPosition: 1 }),
      mockFund({ id: '2', isin: 'LU0996182563', name: 'Fund B', rankingPosition: 2 }),
    ];
    render(<FundTable funds={funds} />);

    const input = screen.getByPlaceholderText(/IE00B4L5Y983/);
    fireEvent.change(input, { target: { value: 'LU099' } });

    expect(screen.queryByText('Fund A')).not.toBeInTheDocument();
    expect(screen.getByText('Fund B')).toBeInTheDocument();
    expect(screen.getByText('1 de 2 fondos')).toBeInTheDocument();
  });

  it('should call onSelectFund on row click', () => {
    const onSelect = vi.fn();
    render(<FundTable funds={[mockFund()]} onSelectFund={onSelect} />);

    fireEvent.click(screen.getByText('iShares Core MSCI World'));
    expect(onSelect).toHaveBeenCalledWith(expect.objectContaining({ isin: 'IE00B4L5Y983' }));
  });

  it('should be case-insensitive on ISIN filter', () => {
    render(<FundTable funds={[mockFund()]} />);
    const input = screen.getByPlaceholderText(/IE00B4L5Y983/);
    fireEvent.change(input, { target: { value: 'ie00b4' } });
    expect(screen.getByText('IE00B4L5Y983')).toBeInTheDocument();
  });
});
