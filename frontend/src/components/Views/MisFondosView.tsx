import { useState, useMemo, useCallback } from 'react';
import { Plus, Trash2, BarChart3, X, Search, Loader2, Trophy, Sparkles } from 'lucide-react';
import {
  RadarChart, Radar, PolarGrid, PolarAngleAxis, PolarRadiusAxis,
  ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend,
} from 'recharts';
import type { Fund } from '../../types/fund';
import { useMyFundsStore, compareFundVsTop, type MyFund } from '../../store/myFundsStore';
import { useConfigStore } from '../../store/configStore';
import { appLog } from '../../store/logStore';
import clsx from 'clsx';

interface AIRanking {
  ranking: { position: number; isin: string; score: number; strengths: string[]; weaknesses: string[] }[];
  bestByHorizon?: {
    shortTerm: { isin: string; name: string; return: number; reason: string };
    mediumTerm: { isin: string; name: string; return: number; reason: string };
    longTerm: { isin: string; name: string; return: number; reason: string };
  };
  summary: string;
  recommendation: string;
}

interface Props {
  topFunds: Fund[];
}

export function MisFondosView({ topFunds }: Props) {
  const { myFunds, addFund, removeFund } = useMyFundsStore();
  const totalFunds = useConfigStore((s) => s.totalFunds);
  const [showForm, setShowForm] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [aiRanking, setAiRanking] = useState<AIRanking | null>(null);
  const [aiLoading, setAiLoading] = useState(false);

  const selectedFund = myFunds.find((f) => f.id === selectedId) ?? null;

  // Calculate scores for local ranking
  const rankedFunds = useMemo(() => {
    return myFunds
      .map((f) => {
        const comparison = compareFundVsTop(f, topFunds, totalFunds);
        return { fund: f, score: comparison?.compositeScore ?? 0, comparison };
      })
      .sort((a, b) => b.score - a.score);
  }, [myFunds, topFunds, totalFunds]);

  const handleAdd = (fund: Omit<MyFund, 'id' | 'addedAt'>) => {
    addFund(fund);
    setShowForm(false);
    setAiRanking(null); // Invalidate AI ranking
    appLog.success('App', `Fondo propio añadido: ${fund.name} (${fund.isin})`);
  };

  const handleRemove = (id: string, name: string) => {
    removeFund(id);
    if (selectedId === id) setSelectedId(null);
    setAiRanking(null);
    appLog.info('App', `Fondo propio eliminado: ${name}`);
  };

  const handleAIAnalysis = useCallback(async () => {
    if (myFunds.length < 2) return;
    setAiLoading(true);
    setAiRanking(null);
    appLog.info('LLM', 'Solicitando análisis comparativo de fondos...');
    try {
      const payload = myFunds.map((f) => ({
        isin: f.isin,
        name: f.name,
        category: f.category,
        managementCompany: f.managementCompany,
        netAssetValue: f.netAssetValue,
        currency: f.currency,
        expenseRatio: f.expenseRatio,
        riskLevel: f.riskLevel,
        return1Year: f.latestPerformance?.return1Year ?? 0,
        return3Years: f.latestPerformance?.return3Years ?? 0,
        return5Years: f.latestPerformance?.return5Years ?? 0,
        volatility: f.latestPerformance?.volatility ?? 0,
        sharpeRatio: f.latestPerformance?.sharpeRatio ?? 0,
      }));
      const res = await fetch('/api/llm/compare', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ funds: payload }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data: AIRanking = await res.json();
      setAiRanking(data);
      appLog.success('LLM', `Análisis completado: ${data.ranking.length} fondos rankeados`);
    } catch (err) {
      appLog.error('LLM', `Error en análisis: ${(err as Error).message}`);
    } finally {
      setAiLoading(false);
    }
  }, [myFunds]);

  return (
    <>
      <div className="mb-4 md:mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h2 className="text-xl md:text-2xl font-bold text-gray-900 dark:text-gray-100">Mis Fondos</h2>
          <p className="text-xs sm:text-sm text-gray-500 dark:text-gray-400 mt-1">
            Añade tus fondos y compáralos con los Top {totalFunds} del mercado
          </p>
        </div>
        <div className="flex gap-2">
          {myFunds.length >= 2 && (
            <button
              onClick={handleAIAnalysis}
              disabled={aiLoading}
              className="flex items-center gap-2 px-3 py-2 bg-purple-600 hover:bg-purple-700 disabled:bg-gray-400 text-white rounded-lg text-xs sm:text-sm font-medium transition-colors"
            >
              {aiLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
              Análisis IA
            </button>
          )}
          <button
            onClick={() => setShowForm(true)}
            className="flex items-center gap-2 px-3 py-2 bg-primary-600 hover:bg-primary-700 text-white rounded-lg text-xs sm:text-sm font-medium transition-colors"
          >
            <Plus className="h-4 w-4" />
            Añadir fondo
          </button>
        </div>
      </div>

      {/* Form modal */}
      {showForm && <AddFundForm onAdd={handleAdd} onCancel={() => setShowForm(false)} />}

      {myFunds.length === 0 ? (
        <div className="bg-white dark:bg-[#2a2a2a] rounded-lg p-12 text-center">
          <BarChart3 className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
          <p className="text-gray-500 dark:text-gray-400 text-lg">No tienes fondos añadidos</p>
          <p className="text-gray-400 dark:text-gray-500 text-sm mt-1">
            Pulsa "Añadir fondo" para introducir los datos de tu inversión
          </p>
        </div>
      ) : (
        <div className="space-y-6">
          {/* AI Analysis Panel */}
          {aiLoading && (
            <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-700/50 rounded-lg p-6 text-center">
              <Loader2 className="h-6 w-6 animate-spin text-purple-500 mx-auto mb-2" />
              <p className="text-sm text-purple-700 dark:text-purple-300">Analizando tus fondos con IA...</p>
            </div>
          )}
          {aiRanking && aiRanking.ranking && aiRanking.ranking.length > 0 && (
            <div className="bg-white dark:bg-[#2a2a2a] rounded-lg border border-purple-200 dark:border-purple-700/50 overflow-hidden">
              <div className="bg-purple-50 dark:bg-purple-900/30 px-5 py-3 border-b border-purple-200 dark:border-purple-700/50 flex items-center gap-2">
                <Trophy className="h-5 w-5 text-purple-600 dark:text-purple-400" />
                <h3 className="text-sm font-bold text-purple-800 dark:text-purple-200">Ranking IA de tus fondos</h3>
              </div>
              <div className="p-5 space-y-4">
                {/* Ranking list */}
                <div className="space-y-3">
                  {aiRanking.ranking.map((r) => {
                    const fund = myFunds.find((f) => f.isin === r.isin);
                    const medal = r.position === 1 ? '🥇' : r.position === 2 ? '🥈' : r.position === 3 ? '🥉' : `#${r.position}`;
                    const scoreColor = r.score >= 70 ? 'text-green-600 dark:text-green-400' : r.score >= 40 ? 'text-yellow-600 dark:text-yellow-400' : 'text-red-600 dark:text-red-400';
                    return (
                      <div key={r.isin} className="flex items-start gap-3 p-3 rounded-lg bg-gray-50 dark:bg-[#1e1e1e]">
                        <span className="text-2xl">{medal}</span>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-sm text-gray-900 dark:text-gray-100 truncate">{fund?.name || r.isin}</span>
                            <span className={clsx('text-sm font-bold', scoreColor)}>{r.score}/100</span>
                          </div>
                          <div className="text-xs text-gray-500 font-mono">{r.isin}</div>
                          <div className="flex flex-wrap gap-1 mt-1">
                            {r.strengths.map((s, i) => (
                              <span key={i} className="text-xs bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 px-2 py-0.5 rounded-full">✓ {s}</span>
                            ))}
                            {r.weaknesses.map((w, i) => (
                              <span key={i} className="text-xs bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 px-2 py-0.5 rounded-full">✗ {w}</span>
                            ))}
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
                {/* Best by Horizon */}
                {aiRanking.bestByHorizon && (
                  <div className="border-t border-gray-200 dark:border-gray-700/50 pt-3">
                    <h4 className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase mb-2">Mejor fondo por horizonte temporal</h4>
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                      {[{ label: 'Corto plazo (1A)', data: aiRanking.bestByHorizon.shortTerm, icon: '⚡' },
                        { label: 'Medio plazo (3A)', data: aiRanking.bestByHorizon.mediumTerm, icon: '📈' },
                        { label: 'Largo plazo (5A)', data: aiRanking.bestByHorizon.longTerm, icon: '🏆' }].map(({ label, data, icon }) => (
                        <div key={label} className="p-3 rounded-lg bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-700/50">
                          <div className="text-xs text-blue-600 dark:text-blue-400 font-medium">{icon} {label}</div>
                          <div className="text-sm font-bold text-gray-900 dark:text-gray-100 mt-1 truncate">{data.name}</div>
                          <div className="text-xs font-mono text-gray-500">{data.isin}</div>
                          <div className="text-sm font-bold text-green-600 dark:text-green-400 mt-1">+{data.return.toFixed(2)}%</div>
                          <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{data.reason}</div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
                {/* Summary & Recommendation */}
                <div className="border-t border-gray-200 dark:border-gray-700/50 pt-3 space-y-2">
                  <p className="text-sm text-gray-700 dark:text-gray-300">{aiRanking.summary}</p>
                  <p className="text-sm font-medium text-purple-700 dark:text-purple-300">
                    💡 {aiRanking.recommendation}
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Fund list */}
          <div className="bg-white dark:bg-[#2a2a2a] rounded-lg shadow overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700/50">
              <thead className="bg-primary-50 dark:bg-primary-900/40">
                <tr>
                  <th className="px-3 md:px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase w-12">Rank</th>
                  <th className="px-3 md:px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden sm:table-cell">ISIN</th>
                  <th className="px-3 md:px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Fondo</th>
                  <th className="px-3 md:px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Categoría</th>
                  <th className="px-3 md:px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden md:table-cell">NAV</th>
                  <th className="px-3 md:px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">1Y</th>
                  <th className="px-3 md:px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden sm:table-cell">TER</th>
                  <th className="px-3 md:px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
                {rankedFunds.map(({ fund, score }, idx) => (
                  <tr
                    key={fund.id}
                    onClick={() => setSelectedId(fund.id === selectedId ? null : fund.id)}
                    className={clsx(
                      'transition-colors cursor-pointer',
                      fund.id === selectedId
                        ? 'bg-primary-50 dark:bg-primary-900/30'
                        : 'hover:bg-blue-50 dark:hover:bg-primary-900/10'
                    )}
                  >
                    <td className="px-3 md:px-4 py-3 text-center">
                      <span className={clsx('text-sm font-bold', score >= 70 ? 'text-green-600 dark:text-green-400' : score >= 40 ? 'text-yellow-600 dark:text-yellow-400' : 'text-red-600 dark:text-red-400')}>
                        #{idx + 1}
                      </span>
                      <div className="text-[10px] text-gray-400">{score}/100</div>
                    </td>
                    <td className="px-3 md:px-4 py-3 text-sm font-mono text-gray-600 dark:text-gray-400 hidden sm:table-cell">{fund.isin}</td>
                    <td className="px-3 md:px-4 py-3">
                      <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{fund.name}</div>
                      <div className="text-xs text-gray-500">{fund.managementCompany}</div>
                      <div className="text-xs text-gray-400 font-mono sm:hidden">{fund.isin}</div>
                    </td>
                    <td className="px-3 md:px-4 py-3 text-sm text-gray-600 dark:text-gray-400 hidden lg:table-cell">{fund.category}</td>
                    <td className="px-3 md:px-4 py-3 text-sm text-right font-mono text-gray-900 dark:text-gray-200 hidden md:table-cell">
                      {fund.netAssetValue.toFixed(2)} {fund.currency}
                    </td>
                    <td className="px-3 md:px-4 py-3 text-sm text-right font-mono">
                      {fund.latestPerformance ? (
                        <span className={clsx(
                          fund.latestPerformance.return1Year >= 0
                            ? 'text-green-600 dark:text-green-400'
                            : 'text-red-600 dark:text-red-400'
                        )}>
                          {fund.latestPerformance.return1Year >= 0 ? '+' : ''}
                          {fund.latestPerformance.return1Year.toFixed(2)}%
                        </span>
                      ) : '—'}
                    </td>
                    <td className="px-3 md:px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400 hidden sm:table-cell">
                      {fund.expenseRatio.toFixed(2)}%
                    </td>
                    <td className="px-3 md:px-4 py-3 text-center">
                      <button
                        onClick={(e) => { e.stopPropagation(); handleRemove(fund.id, fund.name); }}
                        className="p-1.5 rounded hover:bg-red-100 dark:hover:bg-red-900/30 text-red-500 transition-colors"
                        title="Eliminar"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Comparison panel */}
          {selectedFund && (
            <ComparisonPanel fund={selectedFund} topFunds={topFunds} totalFunds={totalFunds} />
          )}
        </div>
      )}
    </>
  );
}

// ── Add Fund Form ──────────────────────────────────────────────────────────

function AddFundForm({ onAdd, onCancel }: {
  onAdd: (fund: Omit<MyFund, 'id' | 'addedAt'>) => void;
  onCancel: () => void;
}) {
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [found, setFound] = useState<Omit<MyFund, 'id' | 'addedAt'> | null>(null);

  const handleSearch = async () => {
    const q = query.trim();
    if (!q) return;

    setLoading(true);
    setError('');
    setFound(null);
    appLog.info('LLM', `Buscando fondo: "${q}"`);

    try {
      const res = await fetch(`/api/llm/lookup?q=${encodeURIComponent(q)}`);
      if (res.status === 404) {
        setError(`No se encontró ningún fondo con "${q}". Prueba con otro ISIN o nombre.`);
        appLog.warn('LLM', `Fondo no encontrado: "${q}"`);
        return;
      }
      if (!res.ok) throw new Error(`Error ${res.status}`);

      const data = await res.json();
      appLog.success('LLM', `Fondo encontrado: ${data.name} (${data.isin})`);

      setFound({
        isin: data.isin,
        name: data.name,
        category: data.category || 'Renta Variable Global',
        managementCompany: data.managementCompany || '',
        riskLevel: data.riskLevel || 5,
        netAssetValue: data.netAssetValue || 0,
        currency: data.currency || 'EUR',
        expenseRatio: data.expenseRatio || 0,
        rating: data.rating || 3,
        latestPerformance: data.latestPerformance || null,
      });
    } catch (err) {
      setError(`Error al buscar: ${(err as Error).message}`);
      appLog.error('LLM', `Error buscando fondo: ${(err as Error).message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleSearch();
    }
  };

  const perf = found?.latestPerformance;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm" onClick={onCancel}>
      <div className="bg-white dark:bg-[#1e1e1e] rounded-2xl shadow-2xl w-[90vw] max-w-lg max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700/50 flex items-center justify-between">
          <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">Añadir Fondo Propio</h3>
          <button onClick={onCancel} className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
            <X className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        <div className="p-6 space-y-4">
          {/* Search */}
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-3">
              Introduce el ISIN o el nombre del fondo. La IA buscará todos los datos automáticamente.
            </p>
            <div className="flex gap-2">
              <input
                className="flex-1 px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#1e1e1e] text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                placeholder="IE00B4L5Y983 o iShares Core MSCI World"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={loading}
                autoFocus
              />
              <button
                onClick={handleSearch}
                disabled={loading || !query.trim()}
                className="flex items-center gap-2 px-4 py-2 bg-primary-600 hover:bg-primary-700 disabled:bg-gray-400 text-white rounded-lg text-sm font-medium transition-colors"
              >
                {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Search className="h-4 w-4" />}
                Buscar
              </button>
            </div>
          </div>

          {/* Error */}
          {error && (
            <div className="px-4 py-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-sm text-red-700 dark:text-red-300">
              {error}
            </div>
          )}

          {/* Loading */}
          {loading && (
            <div className="flex items-center justify-center py-8 text-gray-400 gap-3">
              <Loader2 className="h-5 w-5 animate-spin" />
              Buscando datos del fondo con IA...
            </div>
          )}

          {/* Result preview */}
          {found && (
            <div className="border border-gray-200 dark:border-gray-700/50 rounded-lg overflow-hidden">
              <div className="bg-green-50 dark:bg-green-900/20 px-4 py-2 border-b border-gray-200 dark:border-gray-700/50">
                <p className="text-sm font-medium text-green-700 dark:text-green-400">Fondo encontrado</p>
              </div>
              <div className="p-4 space-y-3">
                <div className="flex justify-between items-start">
                  <div>
                    <div className="font-medium text-gray-900 dark:text-gray-100">{found.name}</div>
                    <div className="text-sm text-gray-500 font-mono">{found.isin}</div>
                  </div>
                  <span className="text-xs bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-gray-600 dark:text-gray-400">
                    {found.category}
                  </span>
                </div>
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs">
                  <div><span className="text-gray-400">Gestora:</span> <span className="text-gray-700 dark:text-gray-300">{found.managementCompany}</span></div>
                  <div><span className="text-gray-400">NAV:</span> <span className="text-gray-700 dark:text-gray-300">{found.netAssetValue.toFixed(2)} {found.currency}</span></div>
                  <div><span className="text-gray-400">TER:</span> <span className="text-gray-700 dark:text-gray-300">{found.expenseRatio.toFixed(2)}%</span></div>
                  <div><span className="text-gray-400">Riesgo:</span> <span className="text-gray-700 dark:text-gray-300">{found.riskLevel}/7</span></div>
                </div>
                {perf && (
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2 text-xs border-t border-gray-100 dark:border-gray-700/50 pt-2">
                    <div><span className="text-gray-400">1A:</span> <span className={perf.return1Year >= 0 ? 'text-green-600' : 'text-red-600'}>{perf.return1Year.toFixed(2)}%</span></div>
                    <div><span className="text-gray-400">3A:</span> <span className={perf.return3Years >= 0 ? 'text-green-600' : 'text-red-600'}>{perf.return3Years.toFixed(2)}%</span></div>
                    <div><span className="text-gray-400">5A:</span> <span className={perf.return5Years >= 0 ? 'text-green-600' : 'text-red-600'}>{perf.return5Years.toFixed(2)}%</span></div>
                    <div><span className="text-gray-400">Volatilidad:</span> <span className="text-gray-700 dark:text-gray-300">{perf.volatility.toFixed(2)}%</span></div>
                    <div><span className="text-gray-400">Sharpe:</span> <span className="text-gray-700 dark:text-gray-300">{perf.sharpeRatio.toFixed(2)}</span></div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onCancel} className="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
              Cancelar
            </button>
            <button
              onClick={() => found && onAdd(found)}
              disabled={!found}
              className="px-4 py-2 text-sm rounded-lg bg-primary-600 hover:bg-primary-700 disabled:bg-gray-400 text-white font-medium transition-colors"
            >
              Añadir a Mis Fondos
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Comparison Panel ───────────────────────────────────────────────────────

function ComparisonPanel({ fund, topFunds, totalFunds }: { fund: MyFund; topFunds: Fund[]; totalFunds: number }) {
  const comparison = useMemo(() => compareFundVsTop(fund, topFunds, totalFunds), [fund, topFunds, totalFunds]);

  if (!comparison) {
    return (
      <div className="bg-white dark:bg-[#2a2a2a] rounded-lg p-8 text-center text-gray-400 dark:text-gray-500">
        Añade datos de rentabilidad a tu fondo para poder compararlo con el Top {totalFunds}
      </div>
    );
  }

  const radarData = [
    { metric: 'Rent. 1A', mine: comparison.return1Year.percentile, label: `${comparison.return1Year.mine.toFixed(1)}%` },
    { metric: 'Rent. 3A', mine: comparison.return3Years.percentile, label: `${comparison.return3Years.mine.toFixed(1)}%` },
    { metric: 'Rent. 5A', mine: comparison.return5Years.percentile, label: `${comparison.return5Years.mine.toFixed(1)}%` },
    { metric: 'Sharpe', mine: comparison.sharpeRatio.percentile, label: comparison.sharpeRatio.mine.toFixed(2) },
    { metric: 'Volatilidad', mine: comparison.volatility.percentile, label: `${comparison.volatility.mine.toFixed(1)}%` },
    { metric: 'Coste (TER)', mine: comparison.expenseRatio.percentile, label: `${comparison.expenseRatio.mine.toFixed(2)}%` },
  ];

  const barData = [
    { period: '1 Año', tuFondo: comparison.return1Year.mine, mediaTop: +comparison.return1Year.topAvg.toFixed(2), mejorTop: comparison.return1Year.topBest },
    { period: '3 Años', tuFondo: comparison.return3Years.mine, mediaTop: +comparison.return3Years.topAvg.toFixed(2), mejorTop: comparison.return3Years.topBest },
    { period: '5 Años', tuFondo: comparison.return5Years.mine, mediaTop: +comparison.return5Years.topAvg.toFixed(2), mejorTop: comparison.return5Years.topBest },
  ];

  const vColor = comparison.verdict.color === 'green'
    ? 'border-green-500 bg-green-500/10 text-green-600 dark:text-green-400'
    : comparison.verdict.color === 'yellow'
      ? 'border-yellow-500 bg-yellow-500/10 text-yellow-600 dark:text-yellow-400'
      : 'border-red-500 bg-red-500/10 text-red-600 dark:text-red-400';

  const scoreColor = comparison.compositeScore >= 70
    ? 'text-green-600 dark:text-green-400'
    : comparison.compositeScore >= 40
      ? 'text-yellow-600 dark:text-yellow-400'
      : 'text-red-600 dark:text-red-400';

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">
        Comparación: {fund.name} vs Top {comparison.ranking.total}
      </h3>

      {/* Verdict + ranking summary */}
      <div className={clsx('rounded-lg border-l-4 p-4 flex items-start gap-4', vColor)}>
        <span className="text-3xl">{comparison.verdict.emoji}</span>
        <div className="flex-1">
          <p className="text-base font-bold">{comparison.verdict.headline}</p>
          <p className="text-sm mt-1 opacity-90">{comparison.verdict.detail}</p>
          <div className="flex items-center gap-4 mt-2 text-xs opacity-70">
            <span>Score global: <strong className={scoreColor}>{comparison.compositeScore}/100</strong> (pondera rent. 1A/3A/5A, sharpe, volatilidad, TER)</span>
          </div>
        </div>
      </div>

      {/* Metric cards */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
        {(['return1Year', 'return3Years', 'return5Years', 'volatility', 'sharpeRatio', 'expenseRatio'] as const).map((key) => {
          const data = comparison[key];
          const labels: Record<string, string> = {
            return1Year: 'Rent. 1A', return3Years: 'Rent. 3A', return5Years: 'Rent. 5A',
            volatility: 'Volatilidad', sharpeRatio: 'Sharpe', expenseRatio: 'TER',
          };
          const isPercent = key !== 'sharpeRatio';
          const fmt = (v: number) => isPercent ? `${v.toFixed(2)}%` : v.toFixed(2);
          const pColor = data.percentile >= 70 ? 'text-green-600 dark:text-green-400'
            : data.percentile >= 40 ? 'text-yellow-600 dark:text-yellow-400'
              : 'text-red-600 dark:text-red-400';

          return (
            <div key={key} className="bg-white dark:bg-[#2a2a2a] rounded-lg p-3 border border-gray-200 dark:border-gray-700/50">
              <div className="text-xs text-gray-500 dark:text-gray-400 uppercase">{labels[key]}</div>
              <div className="text-lg font-bold text-gray-900 dark:text-gray-100 mt-1">{fmt(data.mine)}</div>
              <div className="text-xs text-gray-400 mt-1">
                Media: {fmt(data.topAvg)} · Mejor: {fmt(data.topBest)}
              </div>
              <div className={clsx('text-xs font-semibold mt-1', pColor)}>
                Percentil {data.percentile}
              </div>
            </div>
          );
        })}
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Radar */}
        <div className="bg-white dark:bg-[#2a2a2a] rounded-lg p-4 border border-gray-200 dark:border-gray-700/50">
          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Posición Relativa (Percentil)</h4>
          <ResponsiveContainer width="100%" height={300}>
            <RadarChart data={radarData}>
              <PolarGrid strokeOpacity={0.3} />
              <PolarAngleAxis dataKey="metric" tick={{ fill: '#9ca3af', fontSize: 11 }} />
              <PolarRadiusAxis domain={[0, 100]} tick={{ fill: '#6b7280', fontSize: 10 }} />
              <Radar name="Tu fondo" dataKey="mine" stroke="#3b82f6" fill="#3b82f6" fillOpacity={0.3} isAnimationActive={false} />
            </RadarChart>
          </ResponsiveContainer>
          <p className="text-xs text-gray-400 text-center mt-1">100 = mejor que todos los Top {totalFunds} · 50 = media</p>
        </div>

        {/* Bar comparison */}
        <div className="bg-white dark:bg-[#2a2a2a] rounded-lg p-4 border border-gray-200 dark:border-gray-700/50">
          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Rentabilidad vs Top {totalFunds}</h4>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={barData} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
              <CartesianGrid strokeDasharray="3 3" strokeOpacity={0.3} />
              <XAxis dataKey="period" tick={{ fill: '#9ca3af', fontSize: 12 }} />
              <YAxis tick={{ fill: '#9ca3af' }} tickFormatter={(v: number) => `${v}%`} />
              <Tooltip
                contentStyle={{ backgroundColor: '#1f2937', border: 'none', borderRadius: '8px', color: '#fff' }}
                formatter={(value: number) => [`${value.toFixed(2)}%`]}
              />
              <Legend />
              <Bar dataKey="tuFondo" name="Tu fondo" fill="#3b82f6" radius={[4, 4, 0, 0]} isAnimationActive={false} />
              <Bar dataKey="mediaTop" name={`Media Top ${totalFunds}`} fill="#6b7280" radius={[4, 4, 0, 0]} isAnimationActive={false} />
              <Bar dataKey="mejorTop" name={`Mejor Top ${totalFunds}`} fill="#10b981" radius={[4, 4, 0, 0]} isAnimationActive={false} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
}
