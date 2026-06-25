import type { Fund } from '../types/fund';
import { RiskLevel, FundRating } from '../types/fund';

function randomBetween(min: number, max: number) {
  return Math.round((Math.random() * (max - min) + min) * 100) / 100;
}

function generateFund(index: number): Fund {
  const names = [
    'iShares Core MSCI World', 'iShares Core S&P 500', 'Amundi Index MSCI World',
    'Vanguard FTSE All-World', 'Amundi MSCI Emerging Markets', 'iShares MSCI EM',
    'Xtrackers Euro Stoxx 50', 'iShares Global Clean Energy', 'Fidelity Global Technology',
    'Cobas Selección FI', 'Indexa RV Mixta Internacional', 'MyInvestor Nasdaq 100',
    'Baelo Dividendo Creciente', 'True Capital FI', 'Magallanes European Equity',
    'azValor Internacional FI', 'Bestinver Internacional', 'Fundsmith Equity',
    'Seilern World Growth', 'Comgest Growth Europe',
  ];
  const categories = [
    'Renta Variable Global', 'Indexado S&P500', 'Tecnología', 'Emergentes',
    'Europa', 'ESG', 'Mixto', 'Renta Fija', 'Asia-Pacífico', 'Materias Primas',
  ];
  const companies = [
    'BlackRock', 'Vanguard', 'Amundi', 'DWS', 'Fidelity',
    'Cobas AM', 'azValor', 'Bestinver', 'Magallanes', 'Indexa Capital',
  ];
  const currencies = ['EUR', 'USD', 'EUR', 'EUR', 'USD'];

  const name = names[index % names.length];
  const return1Y = randomBetween(-5, 35);
  const sharpe = randomBetween(0.2, 3.0);
  const return3Y = randomBetween(return1Y * 0.8, return1Y + 30);
  const return5Y = randomBetween(return3Y * 0.8, return3Y + 40);

  return {
    id: `fund-${String(index + 1).padStart(3, '0')}`,
    isin: `IE00B${String(index).padStart(5, '0')}${index % 2 === 0 ? 'Y' : 'X'}${index}`,
    name: index < 20 ? name : `${name} ${Math.ceil(index / 20)}`,
    category: categories[index % categories.length],
    managementCompany: companies[index % companies.length],
    riskLevel: (Math.floor(index / 15) % 7 + 1) as RiskLevel,
    netAssetValue: randomBetween(10, 600),
    currency: currencies[index % currencies.length],
    expenseRatio: randomBetween(0.05, 2.0),
    rating: Math.min(5, Math.max(0, Math.round(sharpe * 1.8))) as FundRating,
    rankingPosition: index + 1,
    lastUpdated: new Date().toISOString(),
    latestPerformance: {
      return1Month: randomBetween(-3, 5),
      return3Months: randomBetween(-5, 12),
      return6Months: randomBetween(-8, 18),
      return1Year: return1Y,
      return3Years: return3Y,
      return5Years: return5Y,
      volatility: randomBetween(5, 25),
      sharpeRatio: sharpe,
      recordedAt: new Date().toISOString(),
    },
  };
}

let _cachedFunds: Fund[] | null = null;

export function getMockFunds(count = 100): Fund[] {
  if (!_cachedFunds) {
    _cachedFunds = Array.from({ length: count }, (_, i) => generateFund(i));
  }
  return _cachedFunds;
}

/** Simula una variación en tiempo real sobre los fondos mock */
export function simulatePriceUpdate(funds: Fund[]): Fund[] {
  const idx = Math.floor(Math.random() * funds.length);
  const fund = funds[idx];
  const variation = (Math.random() - 0.5) * 2; // -1% a +1%
  const newNav = Math.round((fund.netAssetValue * (1 + variation / 100)) * 100) / 100;

  return funds.map((f, i) =>
    i === idx ? { ...f, netAssetValue: newNav, lastUpdated: new Date().toISOString() } : f
  );
}
