import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { Search, Target, Sparkles, ThumbsDown, Bookmark, Shield, Zap } from 'lucide-react';

interface InvestorProfile {
  id: string;
  riskTolerance: string;
  investmentHorizon: string;
  investmentStyle: string;
  assetPreference: string;
  maxExpenseRatio: number;
  minReturn1Y: number;
  esgPreference: boolean;
  preferredSectors: string;
  preferredGeographies: string;
  excludedSectors: string;
  interactionsCount: number;
}

interface Recommendation {
  id: string;
  entityType: string;
  entityName: string;
  ticker?: string;
  isin?: string;
  reason: string;
  score: number;
  category: string;
  metrics?: string;
  status: string;
  generatedAt: string;
}

export function ScreenerView() {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<'profile' | 'recommendations' | 'free'>('profile');

  // Free screener state
  const [criteria, setCriteria] = useState('');
  const [result, setResult] = useState<string | null>(null);
  const [sources, setSources] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  // Profile form state
  const [formDirty, setFormDirty] = useState(false);
  const [form, setForm] = useState({
    riskTolerance: 'moderate',
    investmentHorizon: 'medium',
    investmentStyle: 'blend',
    assetPreference: 'both',
    maxExpenseRatio: 1.5,
    minReturn1Y: 0,
    esgPreference: false,
    preferredSectors: [] as string[],
    preferredGeographies: [] as string[],
    excludedSectors: [] as string[],
  });

  const { data: profile } = useQuery<InvestorProfile>({
    queryKey: ['investor-profile'],
    queryFn: async () => {
      const res = await axios.get('/api/screener/profile');
      const p = res.data;
      if (!formDirty) {
        setForm({
          riskTolerance: p.riskTolerance,
          investmentHorizon: p.investmentHorizon,
          investmentStyle: p.investmentStyle,
          assetPreference: p.assetPreference,
          maxExpenseRatio: p.maxExpenseRatio,
          minReturn1Y: p.minReturn1Y,
          esgPreference: p.esgPreference,
          preferredSectors: safeParseArray(p.preferredSectors),
          preferredGeographies: safeParseArray(p.preferredGeographies),
          excludedSectors: safeParseArray(p.excludedSectors),
        });
      }
      return p;
    },
  });

  const { data: recommendations = [] } = useQuery<Recommendation[]>({
    queryKey: ['screener-recommendations'],
    queryFn: async () => (await axios.get('/api/screener/recommendations')).data,
  });

  const updateProfile = useMutation({
    mutationFn: () => axios.put('/api/screener/profile', form),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['investor-profile'] });
      setFormDirty(false);
    },
  });

  const generateRecs = useMutation({
    mutationFn: () => axios.post('/api/screener/recommendations/generate'),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['screener-recommendations'] }),
  });

  const dismissRec = useMutation({
    mutationFn: (id: string) => axios.put(`/api/screener/recommendations/${id}/dismiss`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['screener-recommendations'] }),
  });

  const saveRec = useMutation({
    mutationFn: (id: string) => axios.put(`/api/screener/recommendations/${id}/save`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['screener-recommendations'] }),
  });

  const runScreener = async () => {
    if (!criteria.trim() || loading) return;
    setLoading(true);
    setResult(null);
    try {
      const res = await axios.post('/api/chat/agent', { agentName: 'ScreenerAgent', input: criteria.trim() });
      setResult(res.data.output);
      setSources(res.data.sources);
    } catch (err: unknown) {
      setResult(`Error: ${err instanceof Error ? err.message : 'No se pudo ejecutar el screener'}`);
    } finally {
      setLoading(false);
    }
  };

  const categoryIcon = (cat: string) => {
    if (cat === 'core') return <Shield className="h-4 w-4 text-blue-500" />;
    if (cat === 'speculative') return <Zap className="h-4 w-4 text-orange-500" />;
    return <Target className="h-4 w-4 text-purple-500" />;
  };

  const allSectors = ['Technology', 'Healthcare', 'Financial', 'Consumer', 'Energy', 'Industrial', 'Materials', 'Real Estate', 'Utilities', 'Communication'];
  const allGeos = ['USA', 'Europe', 'España', 'Asia', 'Global', 'Emergentes'];

  return (
    <div className="p-4 md:p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Search className="h-6 w-6 text-blue-500" />
          <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">Buscador Inteligente</h2>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 bg-gray-100 dark:bg-gray-800 p-1 rounded-lg w-fit">
        {([['profile', 'Mi Perfil'], ['recommendations', 'Recomendaciones'], ['free', 'Búsqueda Libre']] as const).map(([key, label]) => (
          <button key={key} onClick={() => setTab(key)}
            className={`px-3 py-1.5 text-sm rounded-md transition-colors ${tab === key ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 shadow-sm' : 'text-gray-500 dark:text-gray-400 hover:text-gray-700'}`}>
            {label}
          </button>
        ))}
      </div>

      {/* Tab: Perfil */}
      {tab === 'profile' && (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-5 space-y-4">
          <p className="text-sm text-gray-500 dark:text-gray-400">Define tu perfil de inversor para recibir recomendaciones personalizadas.</p>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div>
              <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Tolerancia al riesgo</label>
              <select value={form.riskTolerance} onChange={e => { setForm(f => ({ ...f, riskTolerance: e.target.value })); setFormDirty(true); }}
                className="w-full mt-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                <option value="conservative">Conservador</option>
                <option value="moderate">Moderado</option>
                <option value="aggressive">Agresivo</option>
              </select>
            </div>
            <div>
              <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Horizonte temporal</label>
              <select value={form.investmentHorizon} onChange={e => { setForm(f => ({ ...f, investmentHorizon: e.target.value })); setFormDirty(true); }}
                className="w-full mt-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                <option value="short">Corto (&lt;2 años)</option>
                <option value="medium">Medio (2-7 años)</option>
                <option value="long">Largo (&gt;7 años)</option>
              </select>
            </div>
            <div>
              <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Estilo de inversión</label>
              <select value={form.investmentStyle} onChange={e => { setForm(f => ({ ...f, investmentStyle: e.target.value })); setFormDirty(true); }}
                className="w-full mt-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                <option value="value">Value</option>
                <option value="growth">Growth</option>
                <option value="blend">Blend</option>
                <option value="income">Income/Dividendos</option>
                <option value="momentum">Momentum</option>
              </select>
            </div>
            <div>
              <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Preferencia de activos</label>
              <select value={form.assetPreference} onChange={e => { setForm(f => ({ ...f, assetPreference: e.target.value })); setFormDirty(true); }}
                className="w-full mt-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                <option value="funds">Solo fondos</option>
                <option value="stocks">Solo acciones</option>
                <option value="both">Ambos</option>
              </select>
            </div>
            <div>
              <label className="text-xs font-medium text-gray-600 dark:text-gray-400">TER máximo (%)</label>
              <input type="number" step="0.1" value={form.maxExpenseRatio}
                onChange={e => { setForm(f => ({ ...f, maxExpenseRatio: +e.target.value })); setFormDirty(true); }}
                className="w-full mt-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
            </div>
            <div>
              <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Rentabilidad mínima 1Y (%)</label>
              <input type="number" step="1" value={form.minReturn1Y}
                onChange={e => { setForm(f => ({ ...f, minReturn1Y: +e.target.value })); setFormDirty(true); }}
                className="w-full mt-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
            </div>
          </div>

          <div className="flex items-center gap-2">
            <input type="checkbox" id="esg" checked={form.esgPreference}
              onChange={e => { setForm(f => ({ ...f, esgPreference: e.target.checked })); setFormDirty(true); }}
              className="rounded" />
            <label htmlFor="esg" className="text-sm text-gray-700 dark:text-gray-300">Solo inversiones ESG/sostenibles</label>
          </div>

          {/* Sectores */}
          <div>
            <label className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-1 block">Sectores preferidos</label>
            <div className="flex flex-wrap gap-1">
              {allSectors.map(s => (
                <button key={s} onClick={() => { setForm(f => ({ ...f, preferredSectors: f.preferredSectors.includes(s) ? f.preferredSectors.filter(x => x !== s) : [...f.preferredSectors, s] })); setFormDirty(true); }}
                  className={`text-xs px-2 py-1 rounded-full border transition-colors ${form.preferredSectors.includes(s) ? 'bg-blue-100 border-blue-300 text-blue-700 dark:bg-blue-900/30 dark:border-blue-700 dark:text-blue-300' : 'border-gray-200 dark:border-gray-600 text-gray-500 hover:border-blue-300'}`}>
                  {s}
                </button>
              ))}
            </div>
          </div>

          <div>
            <label className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-1 block">Geografías preferidas</label>
            <div className="flex flex-wrap gap-1">
              {allGeos.map(g => (
                <button key={g} onClick={() => { setForm(f => ({ ...f, preferredGeographies: f.preferredGeographies.includes(g) ? f.preferredGeographies.filter(x => x !== g) : [...f.preferredGeographies, g] })); setFormDirty(true); }}
                  className={`text-xs px-2 py-1 rounded-full border transition-colors ${form.preferredGeographies.includes(g) ? 'bg-green-100 border-green-300 text-green-700 dark:bg-green-900/30 dark:border-green-700 dark:text-green-300' : 'border-gray-200 dark:border-gray-600 text-gray-500 hover:border-green-300'}`}>
                  {g}
                </button>
              ))}
            </div>
          </div>

          <button onClick={() => updateProfile.mutate()} disabled={!formDirty}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm">
            Guardar Perfil
          </button>

          {profile && profile.interactionsCount > 0 && (
            <p className="text-xs text-gray-400">{profile.interactionsCount} interacciones registradas — el screener aprende de tus decisiones.</p>
          )}
        </div>
      )}

      {/* Tab: Recomendaciones */}
      {tab === 'recommendations' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Recomendaciones personalizadas basadas en tu perfil de inversor.
            </p>
            <button onClick={() => generateRecs.mutate()} disabled={generateRecs.isPending}
              className="flex items-center gap-1 px-3 py-1.5 text-sm bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50">
              <Sparkles className="h-4 w-4" />
              {generateRecs.isPending ? 'Generando...' : 'Generar Nuevas'}
            </button>
          </div>

          {recommendations.length === 0 && (
            <div className="text-center py-8 text-gray-400 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
              <Sparkles className="h-10 w-10 mx-auto mb-2 opacity-30" />
              <p className="text-sm">No hay recomendaciones activas</p>
              <p className="text-xs">Completa tu perfil y genera recomendaciones</p>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {recommendations.map(rec => (
              <div key={rec.id} className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-2">
                    {categoryIcon(rec.category)}
                    <div>
                      <span className="font-medium text-sm text-gray-900 dark:text-gray-100">{rec.entityName}</span>
                      {rec.ticker && <span className="text-xs text-gray-400 ml-1">({rec.ticker})</span>}
                    </div>
                  </div>
                  <div className="flex items-center gap-1">
                    <span className={`text-xs font-bold px-1.5 py-0.5 rounded ${
                      rec.score >= 80 ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' :
                      rec.score >= 60 ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400' :
                      'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                    }`}>{rec.score}%</span>
                  </div>
                </div>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">{rec.reason}</p>
                <div className="flex items-center gap-2 mt-3 border-t border-gray-100 dark:border-gray-700 pt-2">
                  <span className="text-xs text-gray-400 capitalize">{rec.entityType} • {rec.category}</span>
                  <div className="ml-auto flex gap-1">
                    <button onClick={() => saveRec.mutate(rec.id)} className="p-1 text-gray-400 hover:text-blue-500" title="Guardar">
                      <Bookmark className="h-4 w-4" />
                    </button>
                    <button onClick={() => dismissRec.mutate(rec.id)} className="p-1 text-gray-400 hover:text-red-500" title="Descartar">
                      <ThumbsDown className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tab: Búsqueda libre */}
      {tab === 'free' && (
        <div className="space-y-4">
          <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 space-y-3">
            <div className="flex gap-2">
              <input type="text" value={criteria} onChange={e => setCriteria(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && runScreener()}
                placeholder="Describe criterios en lenguaje natural..."
                className="flex-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100"
                disabled={loading} />
              <button onClick={runScreener} disabled={loading || !criteria.trim()}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm whitespace-nowrap">
                {loading ? 'Filtrando...' : 'Filtrar'}
              </button>
            </div>
            <div className="flex flex-wrap gap-1">
              {['Fondos con Sharpe > 1 y TER < 1%', 'Empresas sector tech con crecimiento > 20%', 'Fondos renta variable europea baja volatilidad'].map((f, i) => (
                <button key={i} onClick={() => setCriteria(f)}
                  className="text-xs px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400 rounded hover:bg-gray-200 dark:hover:bg-gray-600">
                  {f}
                </button>
              ))}
            </div>
          </div>
          {result && (
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-5">
              <div className="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap text-sm text-gray-700 dark:text-gray-300 max-h-[50vh] overflow-y-auto">{result}</div>
              {sources && (
                <details className="mt-3 border-t border-gray-100 dark:border-gray-700 pt-2">
                  <summary className="text-xs cursor-pointer text-gray-400">Fuentes</summary>
                  <pre className="text-xs mt-1 text-gray-400">{sources}</pre>
                </details>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function safeParseArray(s: string): string[] {
  try { return JSON.parse(s || '[]'); } catch { return []; }
}
