import { useState, useEffect, useCallback } from 'react';
import { RefreshCw, Save, Server, Database, Zap, Clock, Loader2 } from 'lucide-react';
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
  const setTotalFunds = useConfigStore((s) => s.setTotalFunds);

  const fetchConfig = useCallback(async () => {
    try {
      const res = await fetch('/api/llm/config');
      const data: ConfigResponse = await res.json();
      setConfig(data.config);
      setProviders(data.providers);
      setCache(data.cache);
      setRateLimiter(data.rateLimiter ?? null);
      setTotalFunds(data.config.totalFunds);
    } catch {
      setMessage({ text: 'Error cargando configuración', type: 'error' });
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

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
      </div>
    );
  }

  if (!config) return null;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Configuración LLM</h2>
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
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          <Server className="h-4 w-4" /> Proveedores LLM
        </h3>
        <div className="flex gap-3">
          {providers.map((p) => (
            <div key={p.name} className={clsx(
              'flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium border',
              p.available
                ? 'bg-green-50 border-green-200 text-green-700 dark:bg-green-900/20 dark:border-green-800 dark:text-green-400'
                : 'bg-gray-50 border-gray-200 text-gray-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-500'
            )}>
              <span className={clsx('h-2 w-2 rounded-full', p.available ? 'bg-green-500' : 'bg-gray-400')} />
              {p.name}
            </div>
          ))}
        </div>
      </section>

      {/* Cache Status */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          <Database className="h-4 w-4" /> Estado del caché
        </h3>
        {cache ? (
          <div className="grid grid-cols-4 gap-4">
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
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">⚡ Rate Limiter (TPM)</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <Stat label="Cuota TPM" value={rateLimiter.tpm.toLocaleString()} />
            <Stat label="Threshold (90%)" value={rateLimiter.threshold.toLocaleString()} />
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
              {Math.round((rateLimiter.currentUsage / rateLimiter.threshold) * 100)}% del threshold
            </p>
          </div>
        </section>
      )}

      {/* Worker Config */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
          <Zap className="h-4 w-4" /> Workers paralelos
        </h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <ConfigField label="Total fondos" value={config.totalFunds} onChange={(v) => updateField('totalFunds', v)} min={10} max={500} step={10} />
          <ConfigField label="Batch size" value={config.batchSize} onChange={(v) => updateField('batchSize', v)} min={1} max={25} step={1} />
          <ConfigField label="Workers" value={config.numWorkers} onChange={(v) => updateField('numWorkers', v)} min={1} max={50} step={1} />
          <ConfigField label="Max concurrente" value={config.maxConcurrent} onChange={(v) => updateField('maxConcurrent', v)} min={1} max={10} step={1} />
        </div>
      </section>

      {/* Retry & Timing */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl p-5 border border-gray-200 dark:border-gray-700/50">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
          <Clock className="h-4 w-4" /> Reintentos y tiempos
        </h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <ConfigField label="Max reintentos" value={config.maxRetries} onChange={(v) => updateField('maxRetries', v)} min={0} max={5} step={1} />
          <ConfigField label="Delay base (ms)" value={config.baseDelayMs} onChange={(v) => updateField('baseDelayMs', v)} min={200} max={5000} step={100} />
          <ConfigField label="Stagger (ms)" value={config.staggerMs} onChange={(v) => updateField('staggerMs', v)} min={0} max={2000} step={50} />
          <ConfigField label="Max tokens" value={config.maxTokens} onChange={(v) => updateField('maxTokens', v)} min={1000} max={16000} step={500} />
          <ConfigField label="Cache TTL (min)" value={config.cacheTtlMinutes} onChange={(v) => updateField('cacheTtlMinutes', v)} min={1} max={60} step={1} />
          <ConfigField label="Rate limit (TPM)" value={config.rateLimitTpm} onChange={(v) => updateField('rateLimitTpm', v)} min={10000} max={2000000} step={10000} />
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
