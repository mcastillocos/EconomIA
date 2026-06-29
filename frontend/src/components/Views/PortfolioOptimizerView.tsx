import { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import axios from 'axios';
import { PieChart, TrendingUp, Sparkles } from 'lucide-react';

interface Fund { id: string; name: string; isin: string; }
interface PortfolioWeight { fundId: string; fundName: string; weight: number; return1Y: number; volatility: number; }
interface EfficientFrontierPoint { risk: number; return: number; }
interface OptimizedPortfolio {
  weights: PortfolioWeight[];
  expectedReturn: number;
  expectedRisk: number;
  sharpeRatio: number;
  efficientFrontier: EfficientFrontierPoint[];
}

export default function PortfolioOptimizerView() {
  const [selectedFunds, setSelectedFunds] = useState<string[]>([]);
  const [targetReturn, setTargetReturn] = useState(5);
  const [minimizeRisk, setMinimizeRisk] = useState(true);

  const { data: funds = [] } = useQuery<Fund[]>({
    queryKey: ['funds-list'],
    queryFn: async () => (await axios.get('/api/funds')).data,
  });

  const optimizeMutation = useMutation<OptimizedPortfolio>({
    mutationFn: async () => (await axios.post('/api/portfoliooptimizer/optimize', {
      fundIds: selectedFunds,
      targetReturn,
      minimizeRisk,
    })).data,
  });

  const result = optimizeMutation.data;

  const toggleFund = (id: string) => {
    setSelectedFunds(prev => prev.includes(id) ? prev.filter(f => f !== id) : [...prev, id]);
  };

  return (
    <div className="p-4 md:p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <PieChart className="h-6 w-6 text-indigo-500" />
          <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">Optimizador de Cartera</h2>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Selección de fondos */}
        <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 max-h-[60vh] overflow-y-auto">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
            Selecciona fondos ({selectedFunds.length} seleccionados)
          </h3>
          <div className="space-y-1">
            {funds.slice(0, 50).map(f => (
              <button key={f.id} onClick={() => toggleFund(f.id)}
                className={`w-full text-left px-3 py-2 rounded text-sm transition-colors ${
                  selectedFunds.includes(f.id)
                    ? 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 border border-indigo-300 dark:border-indigo-700'
                    : 'hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300'
                }`}>
                <span className="font-medium">{f.name}</span>
                <span className="text-xs text-gray-400 ml-2">{f.isin}</span>
              </button>
            ))}
          </div>
        </div>

        {/* Parámetros + resultado */}
        <div className="lg:col-span-2 space-y-4">
          {/* Parámetros */}
          <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
            <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Parámetros de optimización</h3>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div>
                <label className="text-xs text-gray-500">Retorno objetivo (%)</label>
                <input type="number" step="0.5" value={targetReturn} onChange={e => setTargetReturn(+e.target.value)}
                  className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
              </div>
              <div>
                <label className="text-xs text-gray-500">Estrategia</label>
                <select value={minimizeRisk ? 'risk' : 'sharpe'} onChange={e => setMinimizeRisk(e.target.value === 'risk')}
                  className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                  <option value="risk">Minimizar riesgo</option>
                  <option value="sharpe">Maximizar Sharpe</option>
                </select>
              </div>
              <div className="flex items-end">
                <button onClick={() => optimizeMutation.mutate()}
                  disabled={selectedFunds.length < 2 || optimizeMutation.isPending}
                  className="w-full px-4 py-1.5 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 flex items-center justify-center gap-2">
                  <Sparkles className="h-4 w-4" />
                  {optimizeMutation.isPending ? 'Optimizando...' : 'Optimizar'}
                </button>
              </div>
            </div>
            {selectedFunds.length < 2 && (
              <p className="text-xs text-amber-600 dark:text-amber-400 mt-2">Selecciona al menos 2 fondos para optimizar.</p>
            )}
          </div>

          {/* Resultado */}
          {result && (
            <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-4">
              <div className="flex items-center gap-3">
                <TrendingUp className="h-5 w-5 text-green-500" />
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Cartera Óptima</h3>
              </div>

              {/* Métricas resumen */}
              <div className="grid grid-cols-3 gap-4">
                <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-3 text-center">
                  <p className="text-2xl font-bold text-green-600 dark:text-green-400">{result.expectedReturn}%</p>
                  <p className="text-xs text-gray-500">Retorno esperado</p>
                </div>
                <div className="bg-red-50 dark:bg-red-900/20 rounded-lg p-3 text-center">
                  <p className="text-2xl font-bold text-red-600 dark:text-red-400">{result.expectedRisk}%</p>
                  <p className="text-xs text-gray-500">Riesgo (volatilidad)</p>
                </div>
                <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3 text-center">
                  <p className="text-2xl font-bold text-blue-600 dark:text-blue-400">{result.sharpeRatio}</p>
                  <p className="text-xs text-gray-500">Sharpe Ratio</p>
                </div>
              </div>

              {/* Pesos */}
              <div>
                <h4 className="text-xs font-semibold text-gray-500 mb-2">Asignación óptima</h4>
                <div className="space-y-2">
                  {result.weights.filter(w => w.weight > 0.5).map(w => (
                    <div key={w.fundId} className="flex items-center gap-3">
                      <div className="flex-1">
                        <div className="flex items-center justify-between mb-0.5">
                          <span className="text-sm text-gray-700 dark:text-gray-300 truncate">{w.fundName}</span>
                          <span className="text-sm font-bold text-indigo-600 dark:text-indigo-400">{w.weight}%</span>
                        </div>
                        <div className="h-2 bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden">
                          <div className="h-full bg-indigo-500 rounded-full" style={{ width: `${w.weight}%` }} />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Frontera eficiente simplificada (texto) */}
              {result.efficientFrontier.length > 0 && (
                <div>
                  <h4 className="text-xs font-semibold text-gray-500 mb-2">Frontera Eficiente ({result.efficientFrontier.length} puntos)</h4>
                  <div className="h-32 bg-gray-50 dark:bg-gray-900 rounded-lg p-2 flex items-end gap-0.5 overflow-hidden">
                    {result.efficientFrontier.slice(0, 40).map((p, i) => (
                      <div key={i} className="flex-1 bg-indigo-400 dark:bg-indigo-600 rounded-t"
                        style={{ height: `${Math.max(5, (p.return / (result.efficientFrontier[result.efficientFrontier.length - 1]?.return || 1)) * 100)}%` }}
                        title={`Riesgo: ${p.risk}% | Retorno: ${p.return}%`}
                      />
                    ))}
                  </div>
                  <div className="flex justify-between text-[10px] text-gray-400 mt-1">
                    <span>← Menor riesgo</span>
                    <span>Mayor riesgo →</span>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
