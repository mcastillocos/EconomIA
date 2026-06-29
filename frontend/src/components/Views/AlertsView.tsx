import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { Bell, BellOff, Plus, Trash2, RotateCcw, AlertTriangle, CheckCircle } from 'lucide-react';

interface Alert {
  id: string;
  name: string;
  entityType: string;
  entityId?: string;
  field: string;
  operator: string;
  threshold: number;
  condition: string;
  isActive: boolean;
  hasTriggered: boolean;
  lastTriggeredAt?: string;
  lastMessage?: string;
  createdAt: string;
}

const FIELDS = [
  { value: 'return1y', label: 'Rentabilidad 1A (%)' },
  { value: 'rating', label: 'Rating (1-5)' },
  { value: 'sharpe', label: 'Sharpe Ratio' },
  { value: 'ter', label: 'TER (%)' },
  { value: 'nav', label: 'NAV' },
  { value: 'volatility', label: 'Volatilidad (%)' },
];

const OPERATORS = [
  { value: '<', label: '< menor que' },
  { value: '>', label: '> mayor que' },
  { value: '<=', label: '≤ menor o igual' },
  { value: '>=', label: '≥ mayor o igual' },
  { value: '==', label: '= igual a' },
];

export default function AlertsView() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', entityType: 'fund', field: 'return1y', operator: '<', threshold: 0 });

  const { data: alerts = [] } = useQuery<Alert[]>({
    queryKey: ['alerts'],
    queryFn: async () => (await axios.get('/api/alerts')).data,
  });

  const createMutation = useMutation({
    mutationFn: (data: typeof form) => axios.post('/api/alerts', data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['alerts'] }); setShowForm(false); setForm({ name: '', entityType: 'fund', field: 'return1y', operator: '<', threshold: 0 }); },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => axios.delete(`/api/alerts/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  const toggleMutation = useMutation({
    mutationFn: (id: string) => axios.put(`/api/alerts/${id}/toggle`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  const resetMutation = useMutation({
    mutationFn: (id: string) => axios.put(`/api/alerts/${id}/reset`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  const triggered = alerts.filter(a => a.hasTriggered);
  const active = alerts.filter(a => a.isActive && !a.hasTriggered);
  const inactive = alerts.filter(a => !a.isActive);

  return (
    <div className="p-4 md:p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Bell className="h-6 w-6 text-orange-500" />
          <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">Alertas</h2>
          {triggered.length > 0 && (
            <span className="px-2 py-0.5 bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-xs rounded-full font-medium">
              {triggered.length} disparada{triggered.length > 1 ? 's' : ''}
            </span>
          )}
        </div>
        <button onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-1 px-3 py-1.5 text-sm bg-orange-600 text-white rounded-lg hover:bg-orange-700">
          <Plus className="h-4 w-4" /> Nueva Alerta
        </button>
      </div>

      {showForm && (
        <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
            <div>
              <label className="text-xs text-gray-500">Nombre*</label>
              <input type="text" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                placeholder="Mi alerta" className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
            </div>
            <div>
              <label className="text-xs text-gray-500">Campo</label>
              <select value={form.field} onChange={e => setForm(f => ({ ...f, field: e.target.value }))}
                className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                {FIELDS.map(f => <option key={f.value} value={f.value}>{f.label}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs text-gray-500">Condición</label>
              <select value={form.operator} onChange={e => setForm(f => ({ ...f, operator: e.target.value }))}
                className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
                {OPERATORS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs text-gray-500">Umbral</label>
              <input type="number" step="0.01" value={form.threshold} onChange={e => setForm(f => ({ ...f, threshold: +e.target.value }))}
                className="w-full px-3 py-1.5 text-sm border border-gray-200 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100" />
            </div>
            <div className="flex items-end">
              <button onClick={() => createMutation.mutate(form)} disabled={!form.name.trim()}
                className="w-full px-3 py-1.5 text-sm bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50">
                Crear
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Alertas disparadas */}
      {triggered.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-red-600 dark:text-red-400 flex items-center gap-1">
            <AlertTriangle className="h-4 w-4" /> Disparadas
          </h3>
          {triggered.map(a => (
            <AlertCard key={a.id} alert={a} onDelete={deleteMutation.mutate} onToggle={toggleMutation.mutate} onReset={resetMutation.mutate} />
          ))}
        </div>
      )}

      {/* Alertas activas */}
      {active.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-green-600 dark:text-green-400 flex items-center gap-1">
            <CheckCircle className="h-4 w-4" /> Activas
          </h3>
          {active.map(a => (
            <AlertCard key={a.id} alert={a} onDelete={deleteMutation.mutate} onToggle={toggleMutation.mutate} onReset={resetMutation.mutate} />
          ))}
        </div>
      )}

      {/* Alertas inactivas */}
      {inactive.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-gray-500 dark:text-gray-400 flex items-center gap-1">
            <BellOff className="h-4 w-4" /> Inactivas
          </h3>
          {inactive.map(a => (
            <AlertCard key={a.id} alert={a} onDelete={deleteMutation.mutate} onToggle={toggleMutation.mutate} onReset={resetMutation.mutate} />
          ))}
        </div>
      )}

      {alerts.length === 0 && !showForm && (
        <div className="text-center py-12 text-gray-400">
          <Bell className="h-12 w-12 mx-auto mb-3 opacity-30" />
          <p className="text-sm">No hay alertas configuradas</p>
          <p className="text-xs mt-1">Crea una para recibir notificaciones cuando se cumplan tus condiciones.</p>
        </div>
      )}
    </div>
  );
}

function AlertCard({ alert, onDelete, onToggle, onReset }: { alert: Alert; onDelete: (id: string) => void; onToggle: (id: string) => void; onReset: (id: string) => void }) {
  return (
    <div className={`flex items-center justify-between p-3 rounded-lg border ${
      alert.hasTriggered
        ? 'bg-red-50 dark:bg-red-900/10 border-red-200 dark:border-red-800'
        : alert.isActive
          ? 'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700'
          : 'bg-gray-50 dark:bg-gray-900 border-gray-200 dark:border-gray-700 opacity-60'
    }`}>
      <div className="flex-1">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-gray-800 dark:text-gray-200">{alert.name}</span>
          <span className="text-xs px-2 py-0.5 bg-gray-100 dark:bg-gray-700 rounded text-gray-600 dark:text-gray-400">
            {alert.condition}
          </span>
        </div>
        {alert.lastMessage && (
          <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">{alert.lastMessage}</p>
        )}
        {alert.lastTriggeredAt && (
          <p className="text-[10px] text-gray-400 mt-0.5">Última vez: {new Date(alert.lastTriggeredAt).toLocaleString('es-ES')}</p>
        )}
      </div>
      <div className="flex items-center gap-1">
        {alert.hasTriggered && (
          <button onClick={() => onReset(alert.id)} className="p-1.5 text-gray-400 hover:text-blue-600" title="Resetear">
            <RotateCcw className="h-4 w-4" />
          </button>
        )}
        <button onClick={() => onToggle(alert.id)} className="p-1.5 text-gray-400 hover:text-orange-600" title={alert.isActive ? 'Desactivar' : 'Activar'}>
          {alert.isActive ? <Bell className="h-4 w-4" /> : <BellOff className="h-4 w-4" />}
        </button>
        <button onClick={() => onDelete(alert.id)} className="p-1.5 text-gray-400 hover:text-red-600" title="Eliminar">
          <Trash2 className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
