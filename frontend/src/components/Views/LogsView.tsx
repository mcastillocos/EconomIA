import { useEffect, useRef, useState } from 'react';
import { useLogStore, type LogLevel, type LogSource } from '../../store/logStore';
import { Trash2, Pause, Play, ArrowDown } from 'lucide-react';
import clsx from 'clsx';

const LEVEL_STYLES: Record<LogLevel, { bg: string; text: string; label: string }> = {
  info:    { bg: 'bg-blue-100 dark:bg-blue-900/40',   text: 'text-blue-700 dark:text-blue-300',   label: 'INFO' },
  warn:    { bg: 'bg-yellow-100 dark:bg-yellow-900/40', text: 'text-yellow-700 dark:text-yellow-300', label: 'WARN' },
  error:   { bg: 'bg-red-100 dark:bg-red-900/40',     text: 'text-red-700 dark:text-red-300',     label: 'ERROR' },
  debug:   { bg: 'bg-gray-100 dark:bg-gray-800',      text: 'text-gray-600 dark:text-gray-400',   label: 'DEBUG' },
  success: { bg: 'bg-green-100 dark:bg-green-900/40', text: 'text-green-700 dark:text-green-300', label: 'OK' },
};

const SOURCE_COLORS: Record<LogSource, string> = {
  LLM:     'text-purple-600 dark:text-purple-400',
  API:     'text-cyan-600 dark:text-cyan-400',
  SignalR: 'text-orange-600 dark:text-orange-400',
  App:     'text-blue-600 dark:text-blue-400',
  Theme:   'text-pink-600 dark:text-pink-400',
  System:  'text-gray-600 dark:text-gray-400',
};

const ALL_LEVELS: LogLevel[] = ['info', 'warn', 'error', 'debug', 'success'];
const ALL_SOURCES: LogSource[] = ['LLM', 'API', 'SignalR', 'App', 'Theme', 'System'];

function formatTime(d: Date): string {
  return d.toLocaleTimeString('es-ES', { hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit', fractionalSecondDigits: 3 } as Intl.DateTimeFormatOptions);
}

export function LogsView() {
  const entries = useLogStore((s) => s.entries);
  const clear = useLogStore((s) => s.clear);
  const [paused, setPaused] = useState(false);
  const [filterLevel, setFilterLevel] = useState<LogLevel | 'all'>('all');
  const [filterSource, setFilterSource] = useState<LogSource | 'all'>('all');
  const [search, setSearch] = useState('');
  const bottomRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const [autoScroll, setAutoScroll] = useState(true);

  // Snapshot entries when paused
  const [snapshot, setSnapshot] = useState(entries);
  useEffect(() => {
    if (!paused) setSnapshot(entries);
  }, [entries, paused]);

  const displayed = snapshot.filter((e) => {
    if (filterLevel !== 'all' && e.level !== filterLevel) return false;
    if (filterSource !== 'all' && e.source !== filterSource) return false;
    if (search && !e.message.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  // Auto-scroll to bottom on new entries
  useEffect(() => {
    if (autoScroll && !paused && bottomRef.current) {
      bottomRef.current.scrollIntoView?.({ behavior: 'smooth' });
    }
  }, [displayed.length, autoScroll, paused]);

  const handleScroll = () => {
    if (!containerRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = containerRef.current;
    setAutoScroll(scrollHeight - scrollTop - clientHeight < 60);
  };

  const counts = {
    total: snapshot.length,
    error: snapshot.filter((e) => e.level === 'error').length,
    warn: snapshot.filter((e) => e.level === 'warn').length,
  };

  return (
    <>
      {/* Header */}
      <div className="mb-4">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
          Logs en Tiempo Real
        </h2>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
          Actividad del sistema: LLM, API, SignalR y eventos de la aplicación
        </p>
      </div>

      {/* Stats bar */}
      <div className="flex items-center gap-3 mb-3 text-sm">
        <span className="text-gray-500 dark:text-gray-400">
          {counts.total} entradas
        </span>
        {counts.error > 0 && (
          <span className="text-red-600 dark:text-red-400 font-medium">
            {counts.error} errores
          </span>
        )}
        {counts.warn > 0 && (
          <span className="text-yellow-600 dark:text-yellow-400 font-medium">
            {counts.warn} warnings
          </span>
        )}
        <span className="text-gray-400 dark:text-gray-600">|</span>
        <span className="text-gray-500 dark:text-gray-400">
          Mostrando {displayed.length}
        </span>
      </div>

      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-2 mb-3">
        {/* Search */}
        <input
          type="text"
          placeholder="Buscar en logs..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#1e1e1e] text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 w-52"
        />

        {/* Level filter */}
        <select
          value={filterLevel}
          onChange={(e) => setFilterLevel(e.target.value as LogLevel | 'all')}
          className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#1e1e1e] text-gray-900 dark:text-gray-100"
        >
          <option value="all">Todos los niveles</option>
          {ALL_LEVELS.map((l) => (
            <option key={l} value={l}>{LEVEL_STYLES[l].label}</option>
          ))}
        </select>

        {/* Source filter */}
        <select
          value={filterSource}
          onChange={(e) => setFilterSource(e.target.value as LogSource | 'all')}
          className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#1e1e1e] text-gray-900 dark:text-gray-100"
        >
          <option value="all">Todas las fuentes</option>
          {ALL_SOURCES.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>

        <div className="flex-1" />

        {/* Actions */}
        <button
          onClick={() => setPaused(!paused)}
          className={clsx(
            'flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg border transition-colors',
            paused
              ? 'border-yellow-400 bg-yellow-50 dark:bg-yellow-900/20 text-yellow-700 dark:text-yellow-300'
              : 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700/50'
          )}
          title={paused ? 'Reanudar' : 'Pausar'}
        >
          {paused ? <Play className="h-4 w-4" /> : <Pause className="h-4 w-4" />}
          {paused ? 'Reanudar' : 'Pausar'}
        </button>

        {!autoScroll && (
          <button
            onClick={() => {
              setAutoScroll(true);
              bottomRef.current?.scrollIntoView?.({ behavior: 'smooth' });
            }}
            className="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
            title="Ir al final"
          >
            <ArrowDown className="h-4 w-4" />
          </button>
        )}

        <button
          onClick={clear}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
          title="Limpiar logs"
        >
          <Trash2 className="h-4 w-4" />
          Limpiar
        </button>
      </div>

      {/* Log entries */}
      <div
        ref={containerRef}
        onScroll={handleScroll}
        className="bg-[#1e1e1e] dark:bg-[#111] rounded-lg border border-gray-200 dark:border-gray-700/50 font-mono text-xs overflow-y-auto"
        style={{ height: 'calc(100vh - 320px)', minHeight: '300px' }}
      >
        {displayed.length === 0 ? (
          <div className="flex items-center justify-center h-full text-gray-500 dark:text-gray-600">
            {entries.length === 0
              ? 'Sin logs todavía — la actividad aparecerá aquí en tiempo real'
              : 'Ningún log coincide con los filtros'}
          </div>
        ) : (
          <div className="p-2 space-y-px">
            {/* Reverse to show newest at bottom (entries are stored newest-first) */}
            {[...displayed].reverse().map((entry) => {
              const style = LEVEL_STYLES[entry.level];
              const srcColor = SOURCE_COLORS[entry.source];
              return (
                <div
                  key={entry.id}
                  className="flex items-start gap-2 py-0.5 px-2 rounded hover:bg-white/5 transition-colors"
                >
                  <span className="text-gray-500 whitespace-nowrap select-all">
                    {formatTime(entry.timestamp)}
                  </span>
                  <span
                    className={clsx(
                      'px-1.5 py-0.5 rounded text-[10px] font-bold leading-tight whitespace-nowrap',
                      style.bg, style.text
                    )}
                  >
                    {style.label}
                  </span>
                  <span className={clsx('whitespace-nowrap font-semibold', srcColor)}>
                    [{entry.source}]
                  </span>
                  <span className="text-gray-300 break-all select-all">
                    {entry.message}
                  </span>
                </div>
              );
            })}
            <div ref={bottomRef} />
          </div>
        )}
      </div>

      {paused && (
        <div className="mt-2 text-center text-xs text-yellow-600 dark:text-yellow-400 animate-pulse">
          ⏸ Logs pausados — los nuevos eventos se acumularán en segundo plano
        </div>
      )}
    </>
  );
}
