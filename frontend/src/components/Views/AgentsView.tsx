import { useState } from 'react';
import axios from 'axios';

interface AgentDef {
  name: string;
  status: 'active' | 'planned';
  description: string;
}

interface AgentRunResult {
  runId: string;
  agentName: string;
  status: string;
  output: string;
  sources?: string;
  error?: string;
}

export function AgentsView() {
  const [selectedAgent, setSelectedAgent] = useState<string | null>(null);
  const [input, setInput] = useState('');
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<AgentRunResult | null>(null);

  const agents: AgentDef[] = [
    { name: 'CompanyAnalysisAgent', status: 'active', description: 'Análisis fundamental de empresas' },
    { name: 'FundAnalysisAgent', status: 'active', description: 'Análisis completo de fondos de inversión' },
    { name: 'DailyNewsAgent', status: 'active', description: 'Briefing diario de noticias relevantes' },
    { name: 'FinancialDataExtractorAgent', status: 'active', description: 'Extracción de datos de CSV/Excel/PDF' },
    { name: 'AnnualReportReaderAgent', status: 'active', description: 'Lectura de informes anuales con checklist' },
    { name: 'EarningsCallAgent', status: 'active', description: 'Destilación de earnings calls' },
    { name: 'ScreenerAgent', status: 'active', description: 'Screening inteligente con IA' },
    { name: 'PortfolioBriefingAgent', status: 'active', description: 'Resumen de cartera' },
    { name: 'ComparisonAgent', status: 'active', description: 'Comparativa entre fondos/empresas' },
    { name: 'RiskAgent', status: 'active', description: 'Evaluación de riesgos' },
    { name: 'DataValidationAgent', status: 'active', description: 'Validación automática de datos' },
  ];

  const statusColors: Record<string, string> = {
    planned: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
    active: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300',
  };

  const runAgent = async () => {
    if (!selectedAgent || !input.trim() || running) return;
    setRunning(true);
    setResult(null);
    try {
      const res = await axios.post('/api/chat/agent', {
        agentName: selectedAgent,
        input: input.trim()
      });
      setResult(res.data);
    } catch (err: unknown) {
      setResult({
        runId: '',
        agentName: selectedAgent,
        status: 'failed',
        output: '',
        error: err instanceof Error ? err.message : 'Error de conexión'
      });
    } finally {
      setRunning(false);
    }
  };

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Agentes IA</h2>
      <p className="text-sm text-gray-500 dark:text-gray-400">
        Ejecuta agentes de análisis financiero. Los agentes activos usan datos cargados y LLM para generar informes.
      </p>

      <div className="grid gap-3 md:grid-cols-2">
        {agents.map(agent => (
          <button
            key={agent.name}
            onClick={() => agent.status === 'active' && setSelectedAgent(agent.name)}
            disabled={agent.status !== 'active'}
            className={`text-left bg-white dark:bg-[#2a2a2a] rounded-xl border p-4 flex items-center gap-3 transition-colors ${
              selectedAgent === agent.name
                ? 'border-blue-500 ring-1 ring-blue-500'
                : 'border-gray-200 dark:border-gray-700'
            } ${agent.status === 'active' ? 'hover:border-blue-300 cursor-pointer' : 'opacity-60 cursor-not-allowed'}`}
          >
            <div className="flex-1">
              <h4 className="font-medium text-gray-800 dark:text-gray-200 text-sm">{agent.name}</h4>
              <p className="text-xs text-gray-500 dark:text-gray-400">{agent.description}</p>
            </div>
            <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[agent.status]}`}>
              {agent.status === 'active' ? 'activo' : agent.status === 'planned' ? 'planificado' : agent.status}
            </span>
          </button>
        ))}
      </div>

      {/* Agent execution panel */}
      {selectedAgent && (
        <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3">
          <h3 className="font-medium text-gray-800 dark:text-gray-200">
            Ejecutar: {selectedAgent}
          </h3>
          <div className="flex gap-2">
            <input
              type="text"
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && runAgent()}
              placeholder="Nombre de empresa o fondo a analizar..."
              className="input-field flex-1"
              disabled={running}
            />
            <button
              onClick={runAgent}
              disabled={running || !input.trim()}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {running ? 'Ejecutando...' : 'Ejecutar'}
            </button>
          </div>

          {/* Result */}
          {result && (
            <div className={`rounded-lg p-4 text-sm ${
              result.status === 'completed'
                ? 'bg-green-50 dark:bg-green-900/10 border border-green-200 dark:border-green-800'
                : 'bg-red-50 dark:bg-red-900/10 border border-red-200 dark:border-red-800'
            }`}>
              <div className="flex justify-between items-center mb-2">
                <span className="font-medium">{result.status === 'completed' ? '✅ Completado' : '❌ Error'}</span>
                {result.runId && <span className="text-xs opacity-60">Run: {result.runId.slice(0, 8)}</span>}
              </div>
              {result.output && (
                <div className="whitespace-pre-wrap text-gray-700 dark:text-gray-300 max-h-96 overflow-y-auto">
                  {result.output}
                </div>
              )}
              {result.sources && (
                <details className="mt-2">
                  <summary className="text-xs cursor-pointer text-gray-500">Fuentes</summary>
                  <pre className="text-xs mt-1 text-gray-400 whitespace-pre-wrap">{result.sources}</pre>
                </details>
              )}
              {result.error && <p className="text-red-600 dark:text-red-400">{result.error}</p>}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
