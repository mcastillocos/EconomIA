import { useState, useEffect } from 'react';
import axios from 'axios';
import { ExportButton } from '../Dashboard/ExportButton';

interface ReportSummary {
  id: string;
  entityType: string;
  reportType: string;
  title: string;
  confidence: string | null;
  createdAt: string;
}

interface ReportDetail extends ReportSummary {
  entityId: string | null;
  content: string | null;
  sources: string | null;
}

export function ReportsView() {
  const [reports, setReports] = useState<ReportSummary[]>([]);
  const [selected, setSelected] = useState<ReportDetail | null>(null);
  const [input, setInput] = useState('');
  const [reportType, setReportType] = useState('company');
  const [generating, setGenerating] = useState(false);
  const [loading, setLoading] = useState(true);

  const fetchReports = async () => {
    try {
      const res = await axios.get('/api/reports');
      setReports(res.data);
    } catch { /* empty */ }
    setLoading(false);
  };

  useEffect(() => { fetchReports(); }, []);

  const generateReport = async () => {
    if (!input.trim() || generating) return;
    setGenerating(true);
    try {
      const res = await axios.post('/api/reports/generate', { input: input.trim(), reportType });
      setSelected(res.data);
      setInput('');
      fetchReports();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : 'Error generando informe');
    } finally {
      setGenerating(false);
    }
  };

  const viewReport = async (id: string) => {
    try {
      const res = await axios.get(`/api/reports/${id}`);
      setSelected(res.data);
    } catch { /* empty */ }
  };

  const deleteReport = async (id: string) => {
    if (!confirm('¿Eliminar este informe?')) return;
    await axios.delete(`/api/reports/${id}`);
    if (selected?.id === id) setSelected(null);
    fetchReports();
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Informes IA</h2>
        {selected && <ExportButton endpoint={`/api/export/report/${selected.id}`} label="Exportar informe" />}
      </div>

      {/* Generator */}
      <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Generar nuevo informe</h3>
        <div className="flex gap-2">
          <select
            value={reportType}
            onChange={e => setReportType(e.target.value)}
            className="input-field w-36"
          >
            <option value="company">Empresa</option>
            <option value="fund">Fondo</option>
          </select>
          <input
            type="text"
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && generateReport()}
            placeholder="Nombre de empresa o fondo..."
            className="input-field flex-1"
            disabled={generating}
          />
          <button
            onClick={generateReport}
            disabled={generating || !input.trim()}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors whitespace-nowrap"
          >
            {generating ? 'Generando...' : 'Generar'}
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* List */}
        <div className="lg:col-span-1 space-y-2">
          <h3 className="text-sm font-medium text-gray-600 dark:text-gray-400">
            Historial ({reports.length})
          </h3>
          {loading && <p className="text-sm text-gray-400">Cargando...</p>}
          {reports.length === 0 && !loading && (
            <p className="text-sm text-gray-400">No hay informes generados.</p>
          )}
          {reports.map(r => (
            <div
              key={r.id}
              className={`bg-white dark:bg-[#2a2a2a] rounded-lg border p-3 cursor-pointer transition-colors ${
                selected?.id === r.id ? 'border-blue-500' : 'border-gray-200 dark:border-gray-700 hover:border-blue-300'
              }`}
              onClick={() => viewReport(r.id)}
            >
              <div className="flex justify-between items-start">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{r.title}</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    {r.entityType} • {new Date(r.createdAt).toLocaleDateString('es-ES')}
                  </p>
                </div>
                <button
                  onClick={e => { e.stopPropagation(); deleteReport(r.id); }}
                  className="text-xs text-red-400 hover:text-red-600 ml-2"
                >
                  ✕
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* Detail */}
        <div className="lg:col-span-2">
          {selected ? (
            <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3">
              <div className="flex justify-between items-center">
                <h3 className="font-medium text-gray-800 dark:text-gray-200">{selected.title}</h3>
                {selected.confidence && (
                  <span className="text-xs px-2 py-0.5 rounded-full bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400">
                    Confianza: {selected.confidence}
                  </span>
                )}
              </div>
              <div className="prose prose-sm dark:prose-invert max-w-none max-h-[60vh] overflow-y-auto whitespace-pre-wrap text-sm text-gray-700 dark:text-gray-300">
                {selected.content || 'Sin contenido.'}
              </div>
              {selected.sources && (
                <details className="mt-2">
                  <summary className="text-xs cursor-pointer text-gray-500 dark:text-gray-400">Fuentes citadas</summary>
                  <pre className="text-xs mt-1 text-gray-400 whitespace-pre-wrap">{selected.sources}</pre>
                </details>
              )}
            </div>
          ) : (
            <div className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-8 text-center text-gray-400">
              <div className="text-3xl mb-2">📊</div>
              <p className="text-sm">Selecciona un informe o genera uno nuevo.</p>
            </div>
          )}
        </div>
      </div>

      {/* Disclaimer */}
      <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg p-2 text-xs text-amber-800 dark:text-amber-200">
        economIA es una herramienta de apoyo al análisis financiero. No constituye recomendación de inversión.
      </div>
    </div>
  );
}
