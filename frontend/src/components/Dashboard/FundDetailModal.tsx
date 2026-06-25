import { useMemo } from 'react';
import { X } from 'lucide-react';
import {
  LineChart, Line, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, Legend, BarChart, Bar,
} from 'recharts';
import type { Fund } from '../../types/fund';

interface Props {
  fund: Fund;
  onClose: () => void;
}

/** Generate simulated historical evolution based on current performance data */
function buildEvolutionData(fund: Fund) {
  const perf = fund.latestPerformance;
  if (!perf) return [];

  // Known data points from performance
  const r1m = perf.return1Month;
  const r3m = perf.return3Months;
  const r6m = perf.return6Months;
  const r1y = perf.return1Year;
  const r3y = perf.return3Years;
  const r5y = perf.return5Years;

  // Build monthly points working backwards from current NAV
  const currentNav = fund.netAssetValue;
  const points: { label: string; nav: number; returnPct: number }[] = [];

  // Calculate NAV at each past point
  const navAt = (totalReturn: number) => currentNav / (1 + totalReturn / 100);

  const anchors = [
    { months: 60, totalReturn: r5y, label: '-5A' },
    { months: 36, totalReturn: r3y, label: '-3A' },
    { months: 12, totalReturn: r1y, label: '-1A' },
    { months: 6, totalReturn: r6m, label: '-6M' },
    { months: 3, totalReturn: r3m, label: '-3M' },
    { months: 1, totalReturn: r1m, label: '-1M' },
    { months: 0, totalReturn: 0, label: 'Hoy' },
  ];

  // Interpolate monthly between anchors
  for (let i = 0; i < anchors.length - 1; i++) {
    const from = anchors[i];
    const to = anchors[i + 1];
    const navFrom = navAt(from.totalReturn);
    const navTo = navAt(to.totalReturn);
    const steps = from.months - to.months;

    for (let s = 0; s < steps; s++) {
      const t = s / steps;
      const noise = 1 + (Math.random() - 0.5) * 0.02; // ±1% noise
      const interpolatedNav = (navFrom + (navTo - navFrom) * t) * noise;
      const monthsAgo = from.months - s;
      const date = new Date();
      date.setMonth(date.getMonth() - monthsAgo);
      points.push({
        label: date.toLocaleDateString('es-ES', { month: 'short', year: '2-digit' }),
        nav: +interpolatedNav.toFixed(2),
        returnPct: +((interpolatedNav / navAt(r5y) - 1) * 100).toFixed(2),
      });
    }
  }

  // Add current point
  points.push({
    label: 'Hoy',
    nav: currentNav,
    returnPct: +((currentNav / navAt(r5y) - 1) * 100).toFixed(2),
  });

  return points;
}

function buildPeriodBars(fund: Fund) {
  const p = fund.latestPerformance;
  if (!p) return [];
  return [
    { period: '1M', return: p.return1Month },
    { period: '3M', return: p.return3Months },
    { period: '6M', return: p.return6Months },
    { period: '1A', return: p.return1Year },
    { period: '3A', return: p.return3Years },
    { period: '5A', return: p.return5Years },
  ];
}

export default function FundDetailModal({ fund, onClose }: Props) {
  const evolution = useMemo(() => buildEvolutionData(fund), [fund]);
  const periodBars = useMemo(() => buildPeriodBars(fund), [fund]);
  const perf = fund.latestPerformance;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="bg-white dark:bg-[#1e1e1e] rounded-2xl shadow-2xl w-[90vw] max-w-5xl max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="sticky top-0 bg-white dark:bg-[#1e1e1e] z-10 px-6 py-4 border-b border-gray-200 dark:border-gray-700/50 flex items-start justify-between">
          <div>
            <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">{fund.name}</h2>
            <div className="flex flex-wrap items-center gap-2 sm:gap-3 mt-1 text-sm text-gray-500 dark:text-gray-400">
              <span className="font-mono bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded text-xs sm:text-sm">{fund.isin}</span>
              <span className="text-xs sm:text-sm">{fund.managementCompany}</span>
              <span className="text-xs sm:text-sm">{fund.category}</span>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-500 dark:text-gray-400"
            aria-label="Cerrar"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Key metrics */}
        <div className="px-6 py-4 grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-6 gap-3">
          <MetricCard label="NAV" value={`${fund.netAssetValue.toFixed(2)} ${fund.currency}`} />
          <MetricCard label="TER" value={`${fund.expenseRatio.toFixed(2)}%`} />
          <MetricCard label="Riesgo" value={`${fund.riskLevel}/7`} />
          <MetricCard label="Rating" value={'★'.repeat(fund.rating) + '☆'.repeat(5 - fund.rating)} />
          {perf && (
            <>
              <MetricCard
                label="Volatilidad"
                value={`${perf.volatility.toFixed(2)}%`}
              />
              <MetricCard
                label="Sharpe"
                value={perf.sharpeRatio.toFixed(2)}
              />
            </>
          )}
        </div>

        {perf && (
          <>
            {/* Rentabilidad por período */}
            <div className="px-6 py-4">
              <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                Rentabilidad por Período
              </h3>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={periodBars} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
                  <CartesianGrid strokeDasharray="3 3" strokeOpacity={0.3} />
                  <XAxis dataKey="period" tick={{ fill: '#9ca3af', fontSize: 12 }} />
                  <YAxis tick={{ fill: '#9ca3af' }} tickFormatter={(v: number) => `${v}%`} />
                  <Tooltip
                    contentStyle={{ backgroundColor: '#1f2937', border: 'none', borderRadius: '8px', color: '#fff' }}
                    formatter={(value: number) => [`${value.toFixed(2)}%`, 'Rentabilidad']}
                  />
                  <Bar
                    dataKey="return"
                    isAnimationActive={false}
                    radius={[4, 4, 0, 0]}
                    fill="#3b82f6"
                    label={{ position: 'top', fill: '#9ca3af', fontSize: 11, formatter: (v: number) => `${v.toFixed(1)}%` }}
                  />
                </BarChart>
              </ResponsiveContainer>
            </div>

            {/* Evolución NAV */}
            {evolution.length > 0 && (
              <div className="px-6 py-4">
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                  Evolución del NAV (5 años)
                </h3>
                <ResponsiveContainer width="100%" height={300}>
                  <AreaChart data={evolution} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
                    <defs>
                      <linearGradient id="navGradient" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.3} />
                        <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" strokeOpacity={0.2} />
                    <XAxis
                      dataKey="label"
                      tick={{ fill: '#9ca3af', fontSize: 10 }}
                      interval={Math.max(1, Math.floor(evolution.length / 12))}
                    />
                    <YAxis
                      tick={{ fill: '#9ca3af' }}
                      tickFormatter={(v: number) => `${v}`}
                      domain={['auto', 'auto']}
                    />
                    <Tooltip
                      contentStyle={{ backgroundColor: '#1f2937', border: 'none', borderRadius: '8px', color: '#fff' }}
                      formatter={(value: number) => [`${value.toFixed(2)} ${fund.currency}`, 'NAV']}
                    />
                    <Area
                      type="monotone"
                      dataKey="nav"
                      stroke="#3b82f6"
                      strokeWidth={2}
                      fill="url(#navGradient)"
                      isAnimationActive={false}
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            )}

            {/* Rentabilidad acumulada */}
            {evolution.length > 0 && (
              <div className="px-6 py-4 pb-6">
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                  Rentabilidad Acumulada (%)
                </h3>
                <ResponsiveContainer width="100%" height={250}>
                  <LineChart data={evolution} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
                    <CartesianGrid strokeDasharray="3 3" strokeOpacity={0.2} />
                    <XAxis
                      dataKey="label"
                      tick={{ fill: '#9ca3af', fontSize: 10 }}
                      interval={Math.max(1, Math.floor(evolution.length / 12))}
                    />
                    <YAxis
                      tick={{ fill: '#9ca3af' }}
                      tickFormatter={(v: number) => `${v}%`}
                    />
                    <Tooltip
                      contentStyle={{ backgroundColor: '#1f2937', border: 'none', borderRadius: '8px', color: '#fff' }}
                      formatter={(value: number) => [`${value.toFixed(2)}%`, 'Rentabilidad']}
                    />
                    <Legend />
                    <Line
                      type="monotone"
                      dataKey="returnPct"
                      name="Rentabilidad %"
                      stroke="#10b981"
                      strokeWidth={2}
                      dot={false}
                      isAnimationActive={false}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            )}
          </>
        )}

        {!perf && (
          <div className="px-6 py-12 text-center text-gray-400 dark:text-gray-500">
            No hay datos de rendimiento disponibles para este fondo
          </div>
        )}
      </div>
    </div>
  );
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-gray-50 dark:bg-[#2a2a2a] rounded-lg px-3 py-2">
      <div className="text-xs text-gray-500 dark:text-gray-400 uppercase">{label}</div>
      <div className="text-sm font-semibold text-gray-900 dark:text-gray-100 mt-0.5">{value}</div>
    </div>
  );
}
