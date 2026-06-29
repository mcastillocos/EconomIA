import { useState } from 'react';
import axios from 'axios';
import { Download } from 'lucide-react';

export function BriefingView() {
  const [briefing, setBriefing] = useState<string | null>(null);
  const [sources, setSources] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [topic, setTopic] = useState('');
  const [agentLabel, setAgentLabel] = useState('');

  const generateBriefing = async (agentName: string, label: string) => {
    setLoading(true);
    setBriefing(null);
    setAgentLabel(label);
    try {
      const res = await axios.post('/api/chat/agent', {
        agentName,
        input: topic || 'briefing general'
      });
      setBriefing(res.data.output);
      setSources(res.data.sources);
    } catch (err: unknown) {
      setBriefing(`⚠️ Error: ${err instanceof Error ? err.message : 'No se pudo generar el briefing'}`);
    } finally {
      setLoading(false);
    }
  };

  const handleExportBriefing = async (format: 'pdf' | 'md') => {
    if (!briefing) return;
    try {
      const res = await fetch(`/api/export/briefing?format=${format}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: agentLabel || 'Resumen Diario', content: briefing, sources })
      });
      if (!res.ok) throw new Error('Error en exportación');
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      const disposition = res.headers.get('content-disposition');
      a.download = disposition?.match(/filename="?([^"]+)"?/)?.[1] ?? `briefing.${format}`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (e) {
      console.error('Export error:', e);
    }
  };

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Resumen Diario</h2>

      {/* Controls */}
      <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div className="flex gap-2 mb-3">
          <input
            type="text"
            value={topic}
            onChange={e => setTopic(e.target.value)}
            placeholder="Foco específico (opcional): ej. sector tecnología, NVIDIA..."
            className="input-field flex-1"
            disabled={loading}
          />
        </div>
        <div className="flex gap-2 flex-wrap">
          <button
            onClick={() => generateBriefing('DailyNewsAgent', 'Briefing Diario')}
            disabled={loading}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors text-sm"
          >
            {loading ? 'Generando...' : '📰 Briefing Diario'}
          </button>
          <button
            onClick={() => generateBriefing('PortfolioBriefingAgent', 'Resumen Cartera')}
            disabled={loading}
            className="px-4 py-2 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 transition-colors text-sm"
          >
            {loading ? 'Generando...' : '💼 Resumen Cartera'}
          </button>
        </div>
      </div>

      {/* Result */}
      {briefing && (
        <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-semibold text-gray-600 dark:text-gray-400">{agentLabel}</h3>
            <div className="flex gap-1">
              <button
                onClick={() => handleExportBriefing('pdf')}
                className="flex items-center gap-1 px-2 py-1 text-xs bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-400 transition-colors"
              >
                <Download className="h-3 w-3" /> PDF
              </button>
              <button
                onClick={() => handleExportBriefing('md')}
                className="flex items-center gap-1 px-2 py-1 text-xs bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-400 transition-colors"
              >
                <Download className="h-3 w-3" /> MD
              </button>
            </div>
          </div>
          <div className="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap text-sm text-gray-700 dark:text-gray-300">
            {briefing}
          </div>
          {sources && (
            <details className="mt-4 border-t border-gray-100 dark:border-gray-700 pt-3">
              <summary className="text-xs cursor-pointer text-gray-500 dark:text-gray-400">Fuentes</summary>
              <pre className="text-xs mt-1 text-gray-400 whitespace-pre-wrap">{sources}</pre>
            </details>
          )}
        </div>
      )}

      {!briefing && !loading && (
        <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-6 text-center text-gray-400">
          <div className="text-3xl mb-2">📰</div>
          <p className="text-sm">Genera un briefing para ver el resumen de tus posiciones y novedades.</p>
        </div>
      )}

      <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg p-2 text-xs text-amber-800 dark:text-amber-200">
        economIA es una herramienta de apoyo al análisis. No constituye recomendación de inversión.
      </div>
    </div>
  );
}
