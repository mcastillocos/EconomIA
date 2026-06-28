import { useState } from 'react';
import axios from 'axios';

export function ScreenerView() {
  const [criteria, setCriteria] = useState('');
  const [result, setResult] = useState<string | null>(null);
  const [sources, setSources] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const runScreener = async () => {
    if (!criteria.trim() || loading) return;
    setLoading(true);
    setResult(null);
    try {
      const res = await axios.post('/api/chat/agent', {
        agentName: 'ScreenerAgent',
        input: criteria.trim()
      });
      setResult(res.data.output);
      setSources(res.data.sources);
    } catch (err: unknown) {
      setResult(`⚠️ Error: ${err instanceof Error ? err.message : 'No se pudo ejecutar el screener'}`);
    } finally {
      setLoading(false);
    }
  };

  const presetFilters = [
    'Empresas con ROE > 15% y deuda/EBITDA < 3',
    'Fondos con Sharpe > 1 y TER < 1%',
    'Empresas del sector tecnología con crecimiento revenue > 20%',
    'Fondos de renta variable europea con baja volatilidad',
  ];

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Screener IA</h2>

      {/* Input */}
      <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3">
        <div className="flex gap-2">
          <input
            type="text"
            value={criteria}
            onChange={e => setCriteria(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && runScreener()}
            placeholder="Describe los criterios de filtrado en lenguaje natural..."
            className="input-field flex-1"
            disabled={loading}
          />
          <button
            onClick={runScreener}
            disabled={loading || !criteria.trim()}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors whitespace-nowrap"
          >
            {loading ? 'Filtrando...' : '🔍 Filtrar'}
          </button>
        </div>

        {/* Presets */}
        <div className="flex flex-wrap gap-2">
          {presetFilters.map((f, i) => (
            <button
              key={i}
              onClick={() => setCriteria(f)}
              className="text-xs px-2 py-1 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 rounded hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
            >
              {f}
            </button>
          ))}
        </div>
      </div>

      {/* Results */}
      {result && (
        <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div className="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap text-sm text-gray-700 dark:text-gray-300 max-h-[60vh] overflow-y-auto">
            {result}
          </div>
          {sources && (
            <details className="mt-4 border-t border-gray-100 dark:border-gray-700 pt-3">
              <summary className="text-xs cursor-pointer text-gray-500 dark:text-gray-400">Datos utilizados</summary>
              <pre className="text-xs mt-1 text-gray-400 whitespace-pre-wrap">{sources}</pre>
            </details>
          )}
        </div>
      )}

      {!result && !loading && (
        <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-6 text-center text-gray-400">
          <div className="text-3xl mb-2">🔍</div>
          <p className="text-sm">Describe tus criterios y el agente filtrará el universo disponible.</p>
          <p className="text-xs mt-1">Funciona sobre datos cargados: sube métricas CSV/Excel para mejores resultados.</p>
        </div>
      )}

      <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg p-2 text-xs text-amber-800 dark:text-amber-200">
        El screener trabaja solo con datos cargados. No accede a fuentes externas en tiempo real.
      </div>
    </div>
  );
}
