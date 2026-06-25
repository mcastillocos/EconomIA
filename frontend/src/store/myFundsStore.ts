import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Fund, FundPerformance } from '../types/fund';

export interface MyFund {
  id: string;
  isin: string;
  name: string;
  category: string;
  managementCompany: string;
  riskLevel: number;
  netAssetValue: number;
  currency: string;
  expenseRatio: number;
  rating: number;
  addedAt: string;
  latestPerformance: FundPerformance | null;
}

interface MyFundsStore {
  myFunds: MyFund[];
  addFund: (fund: Omit<MyFund, 'id' | 'addedAt'>) => void;
  removeFund: (id: string) => void;
  clearAll: () => void;
}

export const useMyFundsStore = create<MyFundsStore>()(
  persist(
    (set) => ({
      myFunds: [],

      addFund: (fund) =>
        set((state) => ({
          myFunds: [
            ...state.myFunds,
            {
              ...fund,
              id: `my-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
              addedAt: new Date().toISOString(),
            },
          ],
        })),

      removeFund: (id) =>
        set((state) => ({
          myFunds: state.myFunds.filter((f) => f.id !== id),
        })),

      clearAll: () => set({ myFunds: [] }),
    }),
    { name: 'economia-my-funds' }
  )
);

/** Compara un fondo propio contra los top N fondos */
export function compareFundVsTop(myFund: MyFund, topFunds: Fund[], totalFundsTarget = 100) {
  const withPerf = topFunds.filter((f) => f.latestPerformance != null);
  if (!myFund.latestPerformance || withPerf.length === 0) return null;

  const mp = myFund.latestPerformance;
  const totalRequested = totalFundsTarget;
  const loaded = withPerf.length;

  const avg = (field: keyof FundPerformance) => {
    const vals = withPerf.map((f) => Number(f.latestPerformance![field])).filter((v) => isFinite(v));
    return vals.length > 0 ? vals.reduce((a, b) => a + b, 0) / vals.length : 0;
  };

  const best = (field: keyof FundPerformance) => {
    const vals = withPerf.map((f) => Number(f.latestPerformance![field])).filter((v) => isFinite(v));
    return vals.length > 0 ? Math.max(...vals) : 0;
  };

  const percentile = (field: keyof FundPerformance, value: number) => {
    const vals = withPerf.map((f) => Number(f.latestPerformance![field])).filter((v) => isFinite(v)).sort((a, b) => a - b);
    if (vals.length === 0) return 0;
    const below = vals.filter((v) => v <= value).length;
    return Math.round((below / vals.length) * 100);
  };

  // Ranking position: where would this fund land if inserted into the top list? (sorted by 1Y return desc)
  const returns1Y = withPerf.map((f) => f.latestPerformance!.return1Year).sort((a, b) => b - a);
  const rankBy1Y = returns1Y.filter((r) => r > mp.return1Year).length + 1;

  // Composite score (weighted average of percentiles)
  const p1Y = percentile('return1Year', mp.return1Year);
  const p3Y = percentile('return3Years', mp.return3Years);
  const p5Y = percentile('return5Years', mp.return5Years);
  const pVol = 100 - percentile('volatility', mp.volatility);
  const pSharpe = percentile('sharpeRatio', mp.sharpeRatio);
  const pTER = 100 - Math.round(
    (withPerf.filter((f) => f.expenseRatio <= myFund.expenseRatio).length / withPerf.length) * 100
  );
  const compositeScore = Math.round((p1Y * 0.25 + p3Y * 0.2 + p5Y * 0.2 + pSharpe * 0.15 + pVol * 0.1 + pTER * 0.1) );

  // Check if fund is actually IN the top list
  const isInTopList = withPerf.some((f) => f.isin === myFund.isin);

  type Verdict = { label: string; color: 'green' | 'yellow' | 'red'; emoji: string; headline: string; detail: string };
  let verdict: Verdict;

  if (isInTopList) {
    verdict = { label: `Top ${totalRequested}`, color: 'green', emoji: '🏆',
      headline: `Está en el Top ${totalRequested} → Posición #${rankBy1Y} por rentabilidad 1A`,
      detail: `Tu fondo forma parte del listado de los ${totalRequested} mejores fondos.` };
  } else {
    // NOT in the top list → position is #101+
    verdict = { label: `Posición #${totalRequested}+`, color: 'red', emoji: '📊',
      headline: `Posición estimada: #${totalRequested}+ (fuera del Top ${totalRequested})`,
      detail: `No fue seleccionado entre los ${totalRequested} mejores. Comparando métricas con los ${loaded} fondos cargados: su rent. 1A (${mp.return1Year.toFixed(1)}%) es similar a la posición #${rankBy1Y} del listado, pero al no estar incluido, su posición real es superior a #${totalRequested}.` };
  }

  const metrics = {
    return1Year: {
      mine: mp.return1Year,
      topAvg: avg('return1Year'),
      topBest: best('return1Year'),
      percentile: p1Y,
    },
    return3Years: {
      mine: mp.return3Years,
      topAvg: avg('return3Years'),
      topBest: best('return3Years'),
      percentile: p3Y,
    },
    return5Years: {
      mine: mp.return5Years,
      topAvg: avg('return5Years'),
      topBest: best('return5Years'),
      percentile: p5Y,
    },
    volatility: {
      mine: mp.volatility,
      topAvg: avg('volatility'),
      topBest: best('volatility'),
      percentile: pVol,
    },
    sharpeRatio: {
      mine: mp.sharpeRatio,
      topAvg: avg('sharpeRatio'),
      topBest: best('sharpeRatio'),
      percentile: pSharpe,
    },
    expenseRatio: {
      mine: myFund.expenseRatio,
      topAvg: withPerf.reduce((a, f) => a + f.expenseRatio, 0) / withPerf.length,
      topBest: Math.min(...withPerf.map((f) => f.expenseRatio)),
      percentile: pTER,
    },
  };

  return {
    ...metrics,
    ranking: { position: rankBy1Y, total: withPerf.length },
    compositeScore,
    verdict,
  };
}
