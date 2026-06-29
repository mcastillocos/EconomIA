import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import axios from 'axios';
import { GitBranch, Play, CheckCircle2, XCircle, Clock, Loader2, ChevronDown, ChevronRight } from 'lucide-react';

interface WorkflowSummary {
  name: string;
  description: string;
  steps: string[];
}

interface WorkflowStepResult {
  stepName: string;
  agentName: string;
  status: string;
  output: string;
  sources?: string;
  error?: string;
}

interface WorkflowResult {
  workflowName: string;
  status: string;
  startedAt: string;
  completedAt?: string;
  finalOutput: string;
  stepResults: WorkflowStepResult[];
}

export default function WorkflowsView() {
  const [selectedWorkflow, setSelectedWorkflow] = useState<string>('');
  const [input, setInput] = useState('');
  const [expandedStep, setExpandedStep] = useState<number | null>(null);

  const { data: workflows = [], isLoading, isError, error } = useQuery<WorkflowSummary[]>({
    queryKey: ['workflows'],
    queryFn: async () => (await axios.get('/api/workflows')).data,
  });

  const mutation = useMutation<WorkflowResult, Error, { name: string; input: string }>({
    mutationFn: async ({ name, input }) => {
      const res = await axios.post(`/api/workflows/${name}`, { input });
      return res.data;
    },
  });

  const handleExecute = () => {
    if (!selectedWorkflow || !input.trim()) return;
    setExpandedStep(null);
    mutation.mutate({ name: selectedWorkflow, input: input.trim() });
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed': return <CheckCircle2 className="h-4 w-4 text-green-500" />;
      case 'failed': return <XCircle className="h-4 w-4 text-red-500" />;
      case 'partial': return <Clock className="h-4 w-4 text-yellow-500" />;
      default: return <Loader2 className="h-4 w-4 text-gray-400 animate-spin" />;
    }
  };

  const selected = workflows.find(w => w.name === selectedWorkflow);

  return (
    <div className="p-4 md:p-6 space-y-6">
      <div className="flex items-center gap-3">
        <GitBranch className="h-6 w-6 text-purple-500" />
        <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">Flujos Multi-Agente</h2>
      </div>

      {/* Estado de carga/error */}
      {isLoading && (
        <div className="flex items-center gap-2 text-gray-500">
          <Loader2 className="h-4 w-4 animate-spin" /> Cargando flujos...
        </div>
      )}
      {isError && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3 text-sm text-red-700 dark:text-red-300">
          Error cargando flujos: {(error as Error)?.message || 'Verifica que el backend esté activo'}
        </div>
      )}
      {!isLoading && !isError && workflows.length === 0 && (
        <div className="text-gray-500 dark:text-gray-400 text-sm">
          No hay flujos disponibles. Asegúrate de que el backend está activo.
        </div>
      )}

      {/* Selector de workflow */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
        {workflows.map((wf) => (
          <button
            key={wf.name}
            onClick={() => setSelectedWorkflow(wf.name)}
            className={`p-4 rounded-lg border text-left transition-all ${
              selectedWorkflow === wf.name
                ? 'border-purple-500 bg-purple-50 dark:bg-purple-900/20 shadow-md'
                : 'border-gray-200 dark:border-gray-700 hover:border-purple-300 dark:hover:border-purple-600'
            }`}
          >
            <div className="font-semibold text-sm text-gray-900 dark:text-gray-100">{wf.name.replace(/_/g, ' ')}</div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1 line-clamp-2">{wf.description}</p>
            <div className="mt-2 flex gap-1 flex-wrap">
              {wf.steps.map((step, i) => (
                <span key={i} className="text-[10px] px-1.5 py-0.5 bg-gray-100 dark:bg-gray-700 rounded text-gray-600 dark:text-gray-300">
                  {i + 1}. {step}
                </span>
              ))}
            </div>
          </button>
        ))}
      </div>

      {/* Input y ejecución */}
      {selected && (
        <div className="space-y-3">
          <div className="text-sm text-gray-600 dark:text-gray-400">
            <strong>{selected.name.replace(/_/g, ' ')}</strong>: {selected.description}
          </div>
          <div className="flex gap-2">
            <input
              type="text"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleExecute()}
              placeholder="Describe qué quieres analizar (ej: 'Inditex', 'ES0113211835', 'Mi cartera global')"
              className="flex-1 px-4 py-2.5 border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm"
            />
            <button
              onClick={handleExecute}
              disabled={mutation.isPending || !input.trim()}
              className="flex items-center gap-2 px-5 py-2.5 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50 font-medium text-sm"
            >
              {mutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Play className="h-4 w-4" />
              )}
              Ejecutar
            </button>
          </div>
        </div>
      )}

      {/* Resultado del workflow */}
      {mutation.data && (
        <div className="space-y-4">
          <div className="flex items-center gap-3 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
            {getStatusIcon(mutation.data.status)}
            <span className="font-medium text-sm text-gray-900 dark:text-gray-100">
              {mutation.data.workflowName.replace(/_/g, ' ')}
            </span>
            <span className={`text-xs px-2 py-0.5 rounded-full ${
              mutation.data.status === 'completed' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' :
              'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
            }`}>
              {mutation.data.status}
            </span>
            {mutation.data.completedAt && (
              <span className="text-xs text-gray-400 ml-auto">
                {((new Date(mutation.data.completedAt).getTime() - new Date(mutation.data.startedAt).getTime()) / 1000).toFixed(1)}s
              </span>
            )}
          </div>

          {/* Pasos */}
          <div className="space-y-2">
            {mutation.data.stepResults.map((step, idx) => (
              <div key={idx} className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
                <button
                  onClick={() => setExpandedStep(expandedStep === idx ? null : idx)}
                  className="w-full flex items-center gap-3 p-3 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
                >
                  {getStatusIcon(step.status)}
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {idx + 1}. {step.stepName}
                  </span>
                  <span className="text-xs text-gray-400">{step.agentName}</span>
                  <span className="ml-auto">
                    {expandedStep === idx ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                  </span>
                </button>
                {expandedStep === idx && (
                  <div className="px-4 pb-4 border-t border-gray-100 dark:border-gray-700">
                    {step.error ? (
                      <p className="text-sm text-red-600 dark:text-red-400 mt-2">{step.error}</p>
                    ) : (
                      <div className="mt-2 text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap max-h-64 overflow-y-auto">
                        {step.output}
                      </div>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Output final */}
          {mutation.data.finalOutput && (
            <div className="p-4 bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-700 rounded-lg">
              <h4 className="text-sm font-semibold text-purple-700 dark:text-purple-300 mb-2">Resultado Final</h4>
              <div className="text-sm text-gray-800 dark:text-gray-200 whitespace-pre-wrap max-h-96 overflow-y-auto">
                {mutation.data.finalOutput}
              </div>
            </div>
          )}
        </div>
      )}

      {mutation.error && (
        <div className="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 rounded-lg">
          <p className="text-sm text-red-700 dark:text-red-400">{mutation.error.message}</p>
        </div>
      )}
    </div>
  );
}
