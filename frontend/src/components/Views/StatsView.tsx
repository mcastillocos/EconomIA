import { useState, useEffect, useCallback } from 'react';
import { BarChart3, Loader2, Euro } from 'lucide-react';
import clsx from 'clsx';

// ── Precios reales (USD por 1M tokens) — de CobolForge ─────────────────────

const PRICES: Record<string, { input: number; output: number }> = {
  'GPT-5.5':  { input: 2.5, output: 10 },
  'GPT-5.4':  { input: 2.5, output: 10 },
  'Claude':   { input: 5.0, output: 25 },
};

const USD_TO_EUR = 0.92;

function calcCostUsd(promptTokens: number, completionTokens: number, provider: string): number {
  const p = PRICES[provider] ?? { input: 5, output: 15 };
  return (promptTokens * p.input) / 1_000_000
       + (completionTokens * p.output) / 1_000_000;
}

// ── Types ──────────────────────────────────────────────────────────────────

interface ProviderStatsInfo {
  calls: number;
  errors: number;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
}

interface StatsResponse {
  providerStats?: Record<string, ProviderStatsInfo>;
}

// ── Component ──────────────────────────────────────────────────────────────

export function StatsView() {
  const [stats, setStats] = useState<Record<string, ProviderStatsInfo>>({});
  const [loading, setLoading] = useState(true);

  const fetchStats = useCallback(async () => {
    try {
      const res = await fetch('/api/llm/config');
      const data: StatsResponse = await res.json();
      setStats(data.providerStats ?? {});
    } catch { /* silencioso */ }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchStats(); }, [fetchStats]);

  useEffect(() => {
    const id = setInterval(fetchStats, 10_000);
    return () => clearInterval(id);
  }, [fetchStats]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
      </div>
    );
  }

  const providerRows = Object.entries(stats).map(([name, s]) => {
    const costUsd = calcCostUsd(s.promptTokens, s.completionTokens, name);
    const costEur = costUsd * USD_TO_EUR;
    return { name, ...s, costUsd, costEur };
  });

  const totals = providerRows.reduce(
    (acc, r) => ({
      calls: acc.calls + r.calls,
      errors: acc.errors + r.errors,
      promptTokens: acc.promptTokens + r.promptTokens,
      completionTokens: acc.completionTokens + r.completionTokens,
      totalTokens: acc.totalTokens + r.totalTokens,
      costUsd: acc.costUsd + r.costUsd,
      costEur: acc.costEur + r.costEur,
    }),
    { calls: 0, errors: 0, promptTokens: 0, completionTokens: 0, totalTokens: 0, costUsd: 0, costEur: 0 },
  );

  const fmtEur = (v: number) => v < 0.01 && v > 0 ? '<0.01 €' : `${v.toFixed(2)} €`;
  const fmtUsd = (v: number) => v < 0.01 && v > 0 ? '<$0.01' : `$${v.toFixed(2)}`;

  return (
    <div className="space-y-6">
      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Estadísticas LLM</h2>

      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label="Llamadas totales" value={totals.calls.toLocaleString()} color="blue" />
        <StatCard label="Errores" value={totals.errors.toLocaleString()} color={totals.errors > 0 ? 'red' : 'gray'} />
        <StatCard label="Tokens totales" value={totals.totalTokens.toLocaleString()} color="purple" />
        <StatCard label="Coste total" value={fmtEur(totals.costEur)} subtitle={fmtUsd(totals.costUsd)} color="green" />
      </div>

      {/* Pricing reference */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          <Euro className="h-4 w-4" /> Precios por proveedor (USD / 1M tokens)
        </h3>
        <div className="flex gap-3 flex-wrap">
          {Object.entries(PRICES).map(([name, p]) => (
            <div key={name} className="px-3 py-2 rounded-lg text-xs font-medium border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 text-gray-700 dark:text-gray-300">
              <span className="font-bold">{name}</span>: entrada ${p.input} · salida ${p.output}
            </div>
          ))}
          <div className="px-3 py-2 rounded-lg text-xs border border-blue-200 dark:border-blue-800 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-400">
            1 USD = {USD_TO_EUR} EUR
          </div>
        </div>
      </section>

      {/* Provider usage + cost table */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          <BarChart3 className="h-4 w-4" /> Uso y coste por proveedor (sesión actual)
        </h3>
        {providerRows.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-700">
                  <th className="text-left py-2 pr-4">Proveedor</th>
                  <th className="text-right py-2 px-3">Llamadas</th>
                  <th className="text-right py-2 px-3">Errores</th>
                  <th className="text-right py-2 px-3">Tokens entrada</th>
                  <th className="text-right py-2 px-3">Tokens salida</th>
                  <th className="text-right py-2 px-3">Total tokens</th>
                  <th className="text-right py-2 px-3">Coste (€)</th>
                </tr>
              </thead>
              <tbody>
                {providerRows.map((r) => (
                  <tr key={r.name} className="border-b border-gray-100 dark:border-gray-800">
                    <td className="py-2 pr-4 font-medium text-gray-900 dark:text-gray-100">{r.name}</td>
                    <td className="py-2 px-3 text-right text-gray-700 dark:text-gray-300">{r.calls.toLocaleString()}</td>
                    <td className="py-2 px-3 text-right">
                      <span className={r.errors > 0 ? 'text-red-500 font-semibold' : 'text-gray-400'}>{r.errors}</span>
                    </td>
                    <td className="py-2 px-3 text-right text-gray-700 dark:text-gray-300">{r.promptTokens.toLocaleString()}</td>
                    <td className="py-2 px-3 text-right text-gray-700 dark:text-gray-300">{r.completionTokens.toLocaleString()}</td>
                    <td className="py-2 px-3 text-right font-semibold text-gray-900 dark:text-gray-100">{r.totalTokens.toLocaleString()}</td>
                    <td className="py-2 px-3 text-right font-semibold text-green-600 dark:text-green-400">{fmtEur(r.costEur)}</td>
                  </tr>
                ))}
                {providerRows.length > 1 && (
                  <tr className="border-t-2 border-gray-300 dark:border-gray-600 font-bold">
                    <td className="py-2 pr-4 text-gray-900 dark:text-gray-100">Total</td>
                    <td className="py-2 px-3 text-right text-gray-900 dark:text-gray-100">{totals.calls.toLocaleString()}</td>
                    <td className="py-2 px-3 text-right">
                      <span className={totals.errors > 0 ? 'text-red-500' : 'text-gray-400'}>{totals.errors}</span>
                    </td>
                    <td className="py-2 px-3 text-right text-gray-900 dark:text-gray-100">{totals.promptTokens.toLocaleString()}</td>
                    <td className="py-2 px-3 text-right text-gray-900 dark:text-gray-100">{totals.completionTokens.toLocaleString()}</td>
                    <td className="py-2 px-3 text-right text-blue-600 dark:text-blue-400">{totals.totalTokens.toLocaleString()}</td>
                    <td className="py-2 px-3 text-right text-green-600 dark:text-green-400 text-base">{fmtEur(totals.costEur)}</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="text-sm text-gray-500 dark:text-gray-400">Sin datos — lanza una carga para ver estadísticas.</p>
        )}
      </section>
    </div>
  );
}

function StatCard({ label, value, subtitle, color }: { label: string; value: string; subtitle?: string; color: string }) {
  const colors: Record<string, string> = {
    blue: 'bg-blue-50 border-blue-200 text-blue-700 dark:bg-blue-900/20 dark:border-blue-800 dark:text-blue-400',
    red: 'bg-red-50 border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-400',
    green: 'bg-green-50 border-green-200 text-green-700 dark:bg-green-900/20 dark:border-green-800 dark:text-green-400',
    purple: 'bg-purple-50 border-purple-200 text-purple-700 dark:bg-purple-900/20 dark:border-purple-800 dark:text-purple-400',
    gray: 'bg-gray-50 border-gray-200 text-gray-500 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400',
  };
  return (
    <div className={clsx('rounded-xl p-4 border', colors[color] ?? colors.gray)}>
      <p className="text-xs opacity-70 mb-1">{label}</p>
      <p className="text-2xl font-bold">{value}</p>
      {subtitle && <p className="text-xs opacity-60 mt-0.5">{subtitle}</p>}
    </div>
  );
}
