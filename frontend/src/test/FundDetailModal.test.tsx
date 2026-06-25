import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import FundDetailModal from '../components/Dashboard/FundDetailModal';
import type { Fund } from '../types/fund';

const fund: Fund = {
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
};

describe('FundDetailModal', () => {
  it('should render fund name and ISIN', () => {
    render(<FundDetailModal fund={fund} onClose={vi.fn()} />);
    expect(screen.getByText('iShares Core MSCI World')).toBeInTheDocument();
    expect(screen.getByText('IE00B4L5Y983')).toBeInTheDocument();
  });

  it('should show management company and category', () => {
    render(<FundDetailModal fund={fund} onClose={vi.fn()} />);
    expect(screen.getByText('BlackRock')).toBeInTheDocument();
    expect(screen.getByText('Renta Variable Global')).toBeInTheDocument();
  });

  it('should display key metrics', () => {
    render(<FundDetailModal fund={fund} onClose={vi.fn()} />);
    expect(screen.getByText('89.34 EUR')).toBeInTheDocument();
    expect(screen.getByText('0.20%')).toBeInTheDocument();
    expect(screen.getByText('5/7')).toBeInTheDocument();
  });

  it('should show period chart section', () => {
    render(<FundDetailModal fund={fund} onClose={vi.fn()} />);
    expect(screen.getByText('Rentabilidad por Período')).toBeInTheDocument();
  });

  it('should show evolution chart sections', () => {
    render(<FundDetailModal fund={fund} onClose={vi.fn()} />);
    expect(screen.getByText('Evolución del NAV (5 años)')).toBeInTheDocument();
    expect(screen.getByText('Rentabilidad Acumulada (%)')).toBeInTheDocument();
  });

  it('should call onClose when close button is clicked', () => {
    const onClose = vi.fn();
    render(<FundDetailModal fund={fund} onClose={onClose} />);
    fireEvent.click(screen.getByLabelText('Cerrar'));
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('should call onClose when backdrop is clicked', () => {
    const onClose = vi.fn();
    const { container } = render(<FundDetailModal fund={fund} onClose={onClose} />);
    // Click on the backdrop (outermost div)
    fireEvent.click(container.firstChild!);
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('should show message when no performance data', () => {
    const noPerf = { ...fund, latestPerformance: null };
    render(<FundDetailModal fund={noPerf} onClose={vi.fn()} />);
    expect(screen.getByText(/no hay datos de rendimiento/i)).toBeInTheDocument();
  });
});
