import { useState, useEffect, useCallback } from 'react';
import { metricsApi } from '../../services/metricsApi';
import type { FinancialMetric, MetricFilter } from '../../types/metric';
import { appLog } from '../../store/logStore';

export function MetricsView() {
  const [metrics, setMetrics] = useState<FinancialMetric[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<MetricFilter>({});

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const data = await metricsApi.getFiltered(filter);
      setMetrics(data);
    } catch (e) {
      appLog.error('App', `Error cargando métricas: ${e}`);
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => { load(); }, [load]);

  const handleValidate = async (id: string, current: boolean) => {
    try {
      if (current) {
        await metricsApi.unvalidate(id);
      } else {
        await metricsApi.validate(id);
      }
      load();
    } catch (e) {
      appLog.error('App', `Error validando métrica: ${e}`);
    }
  };

  const confidenceBadge = (c: string) => {
    const colors: Record<string, string> = {
      high: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300',
      medium: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300',
      low: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300',
    };
    return <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${colors[c] || ''}`}>{c}</span>;
  };

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">Datos Normalizados</h2>

      {/* Filters */}
      <div className="flex flex-wrap gap-2 bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700 p-3">
        <input placeholder="Métrica" value={filter.metricName || ''} onChange={e => setFilter({ ...filter, metricName: e.target.value || undefined })} className="input-field text-sm w-40" />
        <select value={filter.entityType || ''} onChange={e => setFilter({ ...filter, entityType: e.target.value || undefined })} className="input-field text-sm">
          <option value="">Tipo entidad</option>
          <option value="company">Empresa</option>
          <option value="fund">Fondo</option>
          <option value="sector">Sector</option>
        </select>
        <input type="number" placeholder="Año" value={filter.year || ''} onChange={e => setFilter({ ...filter, year: e.target.value ? Number(e.target.value) : undefined })} className="input-field text-sm w-24" />
        <input type="number" placeholder="Q" value={filter.quarter || ''} onChange={e => setFilter({ ...filter, quarter: e.target.value ? Number(e.target.value) : undefined })} className="input-field text-sm w-16" />
        <select value={filter.validated === undefined ? '' : String(filter.validated)} onChange={e => setFilter({ ...filter, validated: e.target.value === '' ? undefined : e.target.value === 'true' })} className="input-field text-sm">
          <option value="">Validación</option>
          <option value="true">Validados</option>
          <option value="false">No validados</option>
        </select>
        <button onClick={load} className="px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700">Filtrar</button>
      </div>

      {/* Disclaimer trazabilidad */}
      <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg p-3 text-xs text-amber-800 dark:text-amber-200">
        <strong>Trazabilidad:</strong> Cada dato muestra su fuente, archivo, página/fila y nivel de confianza. Verifique manualmente los datos marcados con confianza baja.
      </div>

      {loading ? (
        <p className="text-gray-500 dark:text-gray-400">Cargando datos...</p>
      ) : metrics.length === 0 ? (
        <p className="text-gray-500 dark:text-gray-400">No hay datos normalizados. Sube archivos en la sección Uploads.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-xs text-left">
            <thead className="text-xs uppercase bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300">
              <tr>
                <th className="px-2 py-2">Métrica</th>
                <th className="px-2 py-2">Valor</th>
                <th className="px-2 py-2">Entidad</th>
                <th className="px-2 py-2">Periodo</th>
                <th className="px-2 py-2">Fuente</th>
                <th className="px-2 py-2">Archivo</th>
                <th className="px-2 py-2">Pág/Fila</th>
                <th className="px-2 py-2">Confianza</th>
                <th className="px-2 py-2">Validado</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {metrics.map(m => (
                <tr key={m.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                  <td className="px-2 py-1.5 font-medium text-gray-800 dark:text-gray-200">{m.metricName}</td>
                  <td className="px-2 py-1.5 text-gray-700 dark:text-gray-300 font-mono">{m.value.toLocaleString('es-ES')}{m.currency ? ` ${m.currency}` : ''}</td>
                  <td className="px-2 py-1.5 text-gray-600 dark:text-gray-400">{m.entityType}{m.ticker ? ` (${m.ticker})` : ''}</td>
                  <td className="px-2 py-1.5 text-gray-600 dark:text-gray-400">{m.year || '-'}{m.quarter ? `/Q${m.quarter}` : ''}</td>
                  <td className="px-2 py-1.5 text-gray-600 dark:text-gray-400">{m.source || m.sourceType || '-'}</td>
                  <td className="px-2 py-1.5 text-gray-500 dark:text-gray-400 max-w-[120px] truncate" title={m.fileName || ''}>{m.fileName || '-'}</td>
                  <td className="px-2 py-1.5 text-gray-500 dark:text-gray-400">{m.page || '-'}/{m.row || '-'}</td>
                  <td className="px-2 py-1.5">{confidenceBadge(m.confidence)}</td>
                  <td className="px-2 py-1.5">
                    <button
                      onClick={() => handleValidate(m.id, m.validated)}
                      className={`px-2 py-0.5 rounded text-xs ${m.validated ? 'bg-green-600 text-white' : 'bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-300'}`}
                    >
                      {m.validated ? '✓' : '○'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <p className="text-xs text-gray-400 dark:text-gray-500 mt-2">{metrics.length} registros</p>
        </div>
      )}
    </div>
  );
}
