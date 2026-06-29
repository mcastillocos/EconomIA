import { useState, useEffect, useCallback } from 'react';
import { RefreshCw, Save, Server, Database, Zap, Clock, Loader2, Plus, Trash2, Power, PowerOff } from 'lucide-react';
import { useConfigStore } from '../../store/configStore';
import clsx from 'clsx';

interface LLMConfig {
  totalFunds: number;
  batchSize: number;
  numWorkers: number;
  maxConcurrent: number;
  staggerMs: number;
  maxRetries: number;
  baseDelayMs: number;
  maxTokens: number;
  cacheTtlMinutes: number;
  rateLimitTpm: number;
}

interface Provider {
  name: string;
  available: boolean;
  enabled: boolean;
  endpoint?: string;
  model?: string;
  deployment?: string;
}

interface CacheInfo {
  funds: number;
  ageMinutes: number;
  ttlMinutes: number;
  fresh: boolean;
}

interface RateLimiterInfo {
  tpm: number;
  threshold: number;
  currentUsage: number;
  activeReservations: number;
}

interface ConfigResponse {
  config: LLMConfig;
  providers: Provider[];
  cache: CacheInfo | null;
  rateLimiter?: RateLimiterInfo;
}

interface Props {
  onReload: () => void;
}

export function ConfigView({ onReload }: Props) {
  const [config, setConfig] = useState<LLMConfig | null>(null);
  const [providers, setProviders] = useState<Provider[]>([]);
  const [cache, setCache] = useState<CacheInfo | null>(null);
  const [rateLimiter, setRateLimiter] = useState<RateLimiterInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [reloading, setReloading] = useState(false);
  const [message, setMessage] = useState<{ text: string; type: 'ok' | 'error' } | null>(null);
  const [selectedProvider, setSelectedProvider] = useState<string | null>(null);
  const [showAddProvider, setShowAddProvider] = useState(false);
  const [newProvider, setNewProvider] = useState({ name: '', type: 'azure' as 'azure' | 'claude', endpoint: '', deployment: '', model: '', apiKey: '' });
  const setTotalFunds = useConfigStore((s) => s.setTotalFunds);

  const fetchConfig = useCallback(async () => {
    try {
      const res = await fetch('/api/llm/config');
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data: ConfigResponse = await res.json();
      setConfig(data.config);
      setProviders(data.providers);
      setCache(data.cache);
      setRateLimiter(data.rateLimiter ?? null);
      setTotalFunds(data.config.totalFunds);
    } catch {
      setMessage({ text: 'LLM Config no disponible (solo en modo desarrollo)', type: 'error' });
    } finally {
      setLoading(false);
    }
  }, [setTotalFunds]);

  useEffect(() => { fetchConfig(); }, [fetchConfig]);

  // Auto-refresh cache info every 30s
  useEffect(() => {
    const id = setInterval(fetchConfig, 30_000);
    return () => clearInterval(id);
  }, [fetchConfig]);

  const handleSave = async () => {
    if (!config) return;
    setSaving(true);
    setMessage(null);
    try {
      const res = await fetch('/api/llm/config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config),
      });
      const data = await res.json();
      if (data.ok) {
        setConfig(data.config);
        setTotalFunds(data.config.totalFunds);
        setMessage({ text: 'Configuración guardada', type: 'ok' });
      } else {
        setMessage({ text: data.error || 'Error al guardar', type: 'error' });
      }
    } catch {
      setMessage({ text: 'Error de red al guardar', type: 'error' });
    } finally {
      setSaving(false);
    }
  };

  const handleReload = async () => {
    setReloading(true);
    setMessage(null);
    try {
      const res = await fetch('/api/llm/reload', { method: 'POST' });
      const data = await res.json();
      if (data.ok) {
        setMessage({ text: 'Caché invalidada. Recargando workers...', type: 'ok' });
        onReload();
      }
    } catch {
      setMessage({ text: 'Error al recargar', type: 'error' });
    } finally {
      setReloading(false);
      // Refresh config after a brief delay to show new cache state
      setTimeout(fetchConfig, 2000);
    }
  };

  const updateField = (key: keyof LLMConfig, value: number) => {
    if (!config) return;
    setConfig({ ...config, [key]: value });
  };

  const toggleProvider = async (name: string, enabled: boolean) => {
    try {
      const res = await fetch('/api/llm/providers', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, enabled }),
      });
      if (res.ok) {
        setProviders(prev => prev.map(p => p.name === name ? { ...p, enabled } : p));
        setMessage({ text: `${name} ${enabled ? 'activado' : 'desactivado'}`, type: 'ok' });
      }
    } catch {
      setMessage({ text: 'Error al cambiar estado del provider', type: 'error' });
    }
  };

  const addProvider = async () => {
    if (!newProvider.name.trim() || !newProvider.type) return;
    try {
      const res = await fetch('/api/llm/providers', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newProvider),
      });
      const data = await res.json();
      if (res.ok) {
        setMessage({ text: `Provider '${newProvider.name}' añadido`, type: 'ok' });
        setShowAddProvider(false);
        setNewProvider({ name: '', type: 'azure', endpoint: '', deployment: '', model: '', apiKey: '' });
        fetchConfig();
      } else {
        setMessage({ text: data.error || 'Error al añadir', type: 'error' });
      }
    } catch {
      setMessage({ text: 'Error de red al añadir provider', type: 'error' });
    }
  };

  const removeProvider = async (name: string) => {
    try {
      const res = await fetch('/api/llm/providers', {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name }),
      });
      if (res.ok) {
        setProviders(prev => prev.filter(p => p.name !== name));
        setSelectedProvider(null);
        setMessage({ text: `Provider '${name}' eliminado`, type: 'ok' });
      }
    } catch {
      setMessage({ text: 'Error al eliminar provider', type: 'error' });
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
      </div>
    );
  }

  if (!config) {
    return (
      <div className="space-y-6">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Configuración</h2>
        <div className="bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300 px-4 py-3 rounded-lg text-sm">
          {message?.text || 'Configuración LLM no disponible en este entorno (solo modo desarrollo con Vite).'}
        </div>
        <div className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Estado del sistema</h3>
          <ul className="text-sm text-gray-600 dark:text-gray-400 space-y-1">
            <li>• API Backend: <span className="text-green-500">Conectado</span></li>
            <li>• Datos cargados vía API REST (fallback)</li>
            <li>• LLM Workers: No disponibles en producción</li>
          </ul>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <h2 className="text-lg sm:text-xl font-semibold text-gray-900 dark:text-gray-100">Configuración LLM</h2>
        <div className="flex gap-2">
          <button
            onClick={handleSave}
            disabled={saving}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors text-sm font-medium"
          >
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            Guardar
          </button>
          <button
            onClick={handleReload}
            disabled={reloading}
            className="flex items-center gap-2 px-4 py-2 bg-amber-600 text-white rounded-lg hover:bg-amber-700 disabled:opacity-50 transition-colors text-sm font-medium"
          >
            {reloading ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
            Forzar recarga
          </button>
        </div>
      </div>

      {message && (
        <div className={clsx(
          'px-4 py-2 rounded-lg text-sm font-medium',
          message.type === 'ok' ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
            : 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
        )}>
          {message.text}
        </div>
      )}

      {/* Providers */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <div className="flex items-center justify-between mb-3">
          <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300">
            <Server className="h-4 w-4" /> Proveedores LLM
          </h3>
          <button
            onClick={() => setShowAddProvider(!showAddProvider)}
            className="flex items-center gap-1 px-2 py-1 text-xs font-medium text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded transition-colors"
          >
            <Plus className="h-3.5 w-3.5" /> Añadir
          </button>
        </div>

        {/* Add provider form */}
        {showAddProvider && (
          <div className="mb-4 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800 space-y-3">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">Nombre</label>
                <input type="text" value={newProvider.name} onChange={e => setNewProvider({ ...newProvider, name: e.target.value })} placeholder="GPT-4o-mini" className="w-full px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm" />
              </div>
              <div>
                <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">Tipo</label>
                <select value={newProvider.type} onChange={e => setNewProvider({ ...newProvider, type: e.target.value as 'azure' | 'claude' })} className="w-full px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm">
                  <option value="azure">Azure OpenAI</option>
                  <option value="claude">Claude</option>
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">Endpoint (opcional)</label>
                <input type="text" value={newProvider.endpoint} onChange={e => setNewProvider({ ...newProvider, endpoint: e.target.value })} placeholder="https://..." className="w-full px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm" />
              </div>
              <div>
                <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">{newProvider.type === 'claude' ? 'Model' : 'Deployment'}</label>
                <input type="text" value={newProvider.type === 'claude' ? newProvider.model : newProvider.deployment} onChange={e => setNewProvider({ ...newProvider, [newProvider.type === 'claude' ? 'model' : 'deployment']: e.target.value })} placeholder={newProvider.type === 'claude' ? 'claude-opus-4-7' : 'gpt-4o-mini'} className="w-full px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm" />
              </div>
              <div className="sm:col-span-2">
                <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">API Key (opcional — usa la del entorno si vacío)</label>
                <input type="password" value={newProvider.apiKey} onChange={e => setNewProvider({ ...newProvider, apiKey: e.target.value })} placeholder="sk-..." className="w-full px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm" />
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setShowAddProvider(false)} className="px-3 py-1.5 text-xs text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 rounded">Cancelar</button>
              <button onClick={addProvider} disabled={!newProvider.name.trim()} className="px-3 py-1.5 text-xs font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 rounded">Añadir provider</button>
            </div>
          </div>
        )}

        <div className="flex flex-wrap gap-3">
          {providers.map((p) => (
            <button key={p.name} onClick={() => setSelectedProvider(selectedProvider === p.name ? null : p.name)} className={clsx(
              'flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium border cursor-pointer transition-all',
              !p.enabled
                ? 'bg-gray-50 border-gray-200 text-gray-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-500 opacity-60'
                : p.available
                  ? 'bg-green-50 border-green-200 text-green-700 dark:bg-green-900/20 dark:border-green-800 dark:text-green-400'
                  : 'bg-amber-50 border-amber-200 text-amber-600 dark:bg-amber-900/20 dark:border-amber-800 dark:text-amber-400',
              selectedProvider === p.name && 'ring-2 ring-blue-500'
            )}>
              <span className={clsx('h-2 w-2 rounded-full', !p.enabled ? 'bg-gray-400' : p.available ? 'bg-green-500' : 'bg-amber-500')} />
              {p.name}
              {!p.enabled && <span className="text-[10px] opacity-60">(off)</span>}
            </button>
          ))}
        </div>

        {/* Provider detail panel */}
        {selectedProvider && (() => {
          const p = providers.find(pr => pr.name === selectedProvider);
          if (!p) return null;
          return (
            <div className="mt-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 space-y-3">
              <div className="flex justify-between items-center">
                <h4 className="text-sm font-medium text-gray-700 dark:text-gray-200">{p.name}</h4>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => toggleProvider(p.name, !p.enabled)}
                    className={clsx('flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-colors', p.enabled ? 'bg-green-100 text-green-700 hover:bg-green-200 dark:bg-green-900/30 dark:text-green-400' : 'bg-gray-100 text-gray-500 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-400')}
                    title={p.enabled ? 'Desactivar' : 'Activar'}
                  >
                    {p.enabled ? <Power className="h-3 w-3" /> : <PowerOff className="h-3 w-3" />}
                    {p.enabled ? 'Activo' : 'Inactivo'}
                  </button>
                  <button
                    onClick={() => removeProvider(p.name)}
                    className="flex items-center gap-1 px-2 py-1 rounded text-xs font-medium text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-900/20 transition-colors"
                    title="Eliminar provider"
                  >
                    <Trash2 className="h-3 w-3" /> Eliminar
                  </button>
                </div>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
                <div>
                  <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">Endpoint</label>
                  <input
                    type="text"
                    value={p.endpoint || ''}
                    readOnly
                    className="w-full px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 text-xs"
                  />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">{p.name.includes('Claude') ? 'Model' : 'Deployment'}</label>
                  <input
                    type="text"
                    value={p.model || p.deployment || ''}
                    readOnly
                    className="w-full px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 text-xs"
                  />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">API Key</label>
                  <input
                    type="password"
                    value={p.available ? '••••••••••••••••' : ''}
                    readOnly
                    className="w-full px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 text-xs"
                  />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">Estado</label>
                  <p className="text-xs text-gray-600 dark:text-gray-400 py-1.5">
                    {!p.enabled ? '⏸️ Desactivado manualmente' : p.available ? '✅ Operativo — key válida detectada' : '❌ Sin API key — configurar en variables de entorno'}
                  </p>
                </div>
              </div>
            </div>
          );
        })()}
      </section>

      {/* Cache Status */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          <Database className="h-4 w-4" /> Estado del caché
        </h3>
        {cache ? (
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <Stat label="Fondos cargados" value={cache.funds} />
            <Stat label="Antigüedad" value={`${cache.ageMinutes} min`} />
            <Stat label="TTL" value={`${cache.ttlMinutes} min`} />
            <Stat label="Estado" value={cache.fresh ? '✅ Fresco' : '⚠️ Expirado'} />
          </div>
        ) : (
          <p className="text-sm text-gray-500 dark:text-gray-400">Sin caché — se recargará al conectar.</p>
        )}
      </section>

      {/* Rate Limiter */}
      {rateLimiter && (
        <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">⚡ Limitador de tasa (TPM)</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <Stat label="Cuota TPM" value={rateLimiter.tpm.toLocaleString()} />
            <Stat label="Umbral (90%)" value={rateLimiter.threshold.toLocaleString()} />
            <Stat label="Uso actual" value={rateLimiter.currentUsage.toLocaleString()} />
            <Stat label="Reservas activas" value={rateLimiter.activeReservations} />
          </div>
          <div className="mt-3">
            <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
              <div
                className={clsx(
                  'h-full rounded-full transition-all duration-500',
                  rateLimiter.currentUsage / rateLimiter.threshold > 0.8 ? 'bg-red-500' :
                  rateLimiter.currentUsage / rateLimiter.threshold > 0.5 ? 'bg-amber-500' : 'bg-green-500'
                )}
                style={{ width: `${Math.min(100, (rateLimiter.currentUsage / rateLimiter.threshold) * 100)}%` }}
              />
            </div>
            <p className="text-xs text-gray-400 mt-1 text-right">
              {Math.round((rateLimiter.currentUsage / rateLimiter.threshold) * 100)}% del umbral
            </p>
          </div>
        </section>
      )}

      {/* Worker Config */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
          <Zap className="h-4 w-4" /> Trabajadores paralelos
        </h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <ConfigField label="Total fondos" value={config.totalFunds} onChange={(v) => updateField('totalFunds', v)} min={10} max={500} step={10} />
          <ConfigField label="Tamaño lote" value={config.batchSize} onChange={(v) => updateField('batchSize', v)} min={1} max={25} step={1} />
          <ConfigField label="Workers" value={config.numWorkers} onChange={(v) => updateField('numWorkers', v)} min={1} max={50} step={1} />
          <ConfigField label="Máx. concurrente" value={config.maxConcurrent} onChange={(v) => updateField('maxConcurrent', v)} min={1} max={10} step={1} />
        </div>
      </section>

      {/* Retry & Timing */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
          <Clock className="h-4 w-4" /> Reintentos y tiempos
        </h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <ConfigField label="Máx. reintentos" value={config.maxRetries} onChange={(v) => updateField('maxRetries', v)} min={0} max={5} step={1} />
          <ConfigField label="Retardo base (ms)" value={config.baseDelayMs} onChange={(v) => updateField('baseDelayMs', v)} min={200} max={5000} step={100} />
          <ConfigField label="Escalonado (ms)" value={config.staggerMs} onChange={(v) => updateField('staggerMs', v)} min={0} max={2000} step={50} />
          <ConfigField label="Máx. tokens" value={config.maxTokens} onChange={(v) => updateField('maxTokens', v)} min={1000} max={16000} step={500} />
          <ConfigField label="Caché TTL (min)" value={config.cacheTtlMinutes} onChange={(v) => updateField('cacheTtlMinutes', v)} min={1} max={60} step={1} />
          <ConfigField label="Límite TPM" value={config.rateLimitTpm} onChange={(v) => updateField('rateLimitTpm', v)} min={10000} max={2000000} step={10000} />
        </div>
      </section>
    </div>
  );
}

function ConfigField({ label, value, onChange, min, max, step }: {
  label: string; value: number; onChange: (v: number) => void;
  min: number; max: number; step: number;
}) {
  const id = label.toLowerCase().replace(/\s+/g, '-');
  return (
    <div>
      <label htmlFor={id} className="block text-xs text-gray-500 dark:text-gray-400 mb-1">{label}</label>
      <input
        id={id}
        type="number"
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        min={min}
        max={max}
        step={step}
        className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
      />
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string | number }) {
  return (
    <div>
      <p className="text-xs text-gray-500 dark:text-gray-400">{label}</p>
      <p className="text-lg font-semibold text-gray-900 dark:text-gray-100">{value}</p>
    </div>
  );
}
