import { useMemo } from 'react';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  ScatterChart, Scatter, RadarChart, Radar, PolarGrid, PolarAngleAxis, PolarRadiusAxis,
  Legend,
} from 'recharts';
import type { Fund } from '../../types/fund';

interface Props {
  funds: Fund[];
}

/** Safe number coercion — handles strings from LLM JSON */
function num(v: unknown): number {
  const n = Number(v);
  return isFinite(n) ? n : 0;
}

function hasPerf(f: Fund): f is Fund & { latestPerformance: NonNullable<Fund['latestPerformance']> } {
  return f.latestPerformance != null;
}

function TopPerformersChart({ funds }: { funds: Fund[] }) {
  const data = useMemo(() =>
    funds
      .filter(hasPerf)
      .slice(0, 15)
      .map(f => ({
        name: f.name.length > 12 ? f.name.substring(0, 12) + '…' : f.name,
        '1 Año': num(f.latestPerformance.return1Year),
        '3 Años': num(f.latestPerformance.return3Years),
      })),
    [funds]
  );

  if (data.length === 0) return <p className="text-gray-400 text-sm">Sin datos de rendimiento (funds: {funds.length}, con perf: {funds.filter(hasPerf).length})</p>;

  return (
    <div>
      <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
        Rentabilidad Top 15 (1Y vs 3Y)
      </h4>
      <ResponsiveContainer width="100%" height={400}>
        <BarChart data={data} layout="vertical" margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
          <CartesianGrid strokeDasharray="3 3" strokeOpacity={0.3} horizontal={false} />
          <YAxis
            dataKey="name"
            type="category"
            width={80}
            fontSize={10}
            interval={0}
            tick={{ fill: '#9ca3af' }}
          />
          <XAxis
            type="number"
            tick={{ fill: '#9ca3af' }}
            tickFormatter={(v: number) => `${v}%`}
          />
          <Tooltip
            contentStyle={{ backgroundColor: '#1f2937', border: 'none', borderRadius: '8px', color: '#fff' }}
            formatter={(value: number) => [`${value}%`]}
          />
          <Legend wrapperStyle={{ paddingTop: 10 }} />
          <Bar dataKey="1 Año" fill="#3b82f6" radius={[4, 4, 0, 0]} isAnimationActive={false} />
          <Bar dataKey="3 Años" fill="#10b981" radius={[4, 4, 0, 0]} isAnimationActive={false} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

function RiskReturnScatter({ funds }: { funds: Fund[] }) {
  const data = useMemo(() =>
    funds
      .filter(hasPerf)
      .map(f => ({
        name: f.name,
        volatility: num(f.latestPerformance.volatility),
        return1Y: num(f.latestPerformance.return1Year),
      })),
    [funds]
  );

  if (data.length === 0) return <p className="text-gray-400 text-sm">Sin datos de riesgo/retorno</p>;

  return (
    <div>
      <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
        Riesgo vs Retorno (Volatilidad vs Rentabilidad 1Y)
      </h4>
      <ResponsiveContainer width="100%" height={300}>
        <ScatterChart margin={{ top: 10, right: 20, bottom: 30, left: 10 }}>
          <CartesianGrid strokeDasharray="3 3" strokeOpacity={0.3} />
          <XAxis
            type="number"
            dataKey="volatility"
            name="Volatilidad"
            tick={{ fill: '#9ca3af' }}
            domain={['auto', 'auto']}
            label={{ value: 'Volatilidad %', position: 'bottom', fill: '#9ca3af', fontSize: 11, offset: 10 }}
          />
          <YAxis
            type="number"
            dataKey="return1Y"
            name="Retorno 1Y"
            tick={{ fill: '#9ca3af' }}
            domain={['auto', 'auto']}
            label={{ value: 'Retorno 1Y %', angle: -90, position: 'insideLeft', fill: '#9ca3af', fontSize: 11 }}
          />
          <Tooltip
            contentStyle={{ backgroundColor: '#1f2937', border: 'none', borderRadius: '8px', color: '#fff' }}
            formatter={(value: number, name: string) => [`${value}%`, name]}
          />
          <Scatter
            name="Fondos"
            data={data}
            fill="#3b82f6"
            isAnimationActive={false}
          />
        </ScatterChart>
      </ResponsiveContainer>
    </div>
  );
}

function CategoryRadar({ funds }: { funds: Fund[] }) {
  const data = useMemo(() => {
    const categories = [...new Set(funds.map(f => f.category))].slice(0, 8);
    return categories.map(cat => {
      const catFunds = funds.filter(f => f.category === cat);
      const avgReturn = catFunds.reduce((s, f) => s + num(f.latestPerformance?.return1Year), 0) / catFunds.length;
      const avgSharpe = catFunds.reduce((s, f) => s + num(f.latestPerformance?.sharpeRatio), 0) / catFunds.length;
      return {
        category: cat.length > 15 ? cat.substring(0, 15) + '…' : cat,
        Rentabilidad: Math.max(0, num(avgReturn)),
        Sharpe: Math.max(0, num(avgSharpe * 20)),
      };
    });
  }, [funds]);

  if (data.length === 0) return <p className="text-gray-400 text-sm">Sin datos de categoría</p>;

  return (
    <div>
      <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
        Rendimiento por Categoría
      </h4>
      <ResponsiveContainer width="100%" height={300}>
        <RadarChart data={data}>
          <PolarGrid strokeOpacity={0.3} />
          <PolarAngleAxis dataKey="category" tick={{ fill: '#9ca3af', fontSize: 10 }} />
          <PolarRadiusAxis tick={{ fill: '#9ca3af', fontSize: 9 }} />
          <Radar name="Rentabilidad" dataKey="Rentabilidad" stroke="#3b82f6" fill="#3b82f6" fillOpacity={0.3} isAnimationActive={false} />
          <Radar name="Sharpe (×20)" dataKey="Sharpe" stroke="#8b5cf6" fill="#8b5cf6" fillOpacity={0.2} isAnimationActive={false} />
          <Legend wrapperStyle={{ fontSize: 11 }} />
        </RadarChart>
      </ResponsiveContainer>
    </div>
  );
}

export default function PerformanceCharts({ funds }: Props) {
  if (funds.length === 0) return null;

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
      <div className="bg-white dark:bg-[#2a2a2a] rounded-lg shadow p-5 transition-colors duration-300">
        <TopPerformersChart funds={funds} />
      </div>
      <div className="bg-white dark:bg-[#2a2a2a] rounded-lg shadow p-5 transition-colors duration-300">
        <RiskReturnScatter funds={funds} />
      </div>
      <div className="lg:col-span-2 bg-white dark:bg-[#2a2a2a] rounded-lg shadow p-5 transition-colors duration-300">
        <CategoryRadar funds={funds} />
      </div>
    </div>
  );
}
