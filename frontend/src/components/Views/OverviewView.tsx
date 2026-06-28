import { useState, useEffect } from 'react';
import { BarChart3, Building2, Bot, Database, FileText, Upload, TrendingUp, Activity, Briefcase } from 'lucide-react';
import clsx from 'clsx';
import axios from 'axios';

interface SectionStatus {
  label: string;
  icon: typeof BarChart3;
  count: number | string;
  subtitle: string;
  color: string;
  view: string;
}

export function OverviewView({ onNavigate }: { onNavigate: (view: string) => void }) {
  const [sections, setSections] = useState<SectionStatus[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadStatus() {
      const results: SectionStatus[] = [
        { label: 'Fondos', icon: TrendingUp, count: '—', subtitle: 'Top ranking', color: 'text-blue-600 dark:text-blue-400', view: 'global' },
        { label: 'Mis Fondos', icon: Briefcase, count: '—', subtitle: 'Seleccionados', color: 'text-purple-600 dark:text-purple-400', view: 'misfondos' },
        { label: 'Empresas', icon: Building2, count: '—', subtitle: 'Registradas', color: 'text-green-600 dark:text-green-400', view: 'companies' },
        { label: 'Métricas', icon: Database, count: '—', subtitle: 'Datos cargados', color: 'text-amber-600 dark:text-amber-400', view: 'metrics' },
        { label: 'Documentos', icon: Upload, count: '—', subtitle: 'Subidos', color: 'text-indigo-600 dark:text-indigo-400', view: 'uploads' },
        { label: 'Informes', icon: FileText, count: '—', subtitle: 'Generados', color: 'text-pink-600 dark:text-pink-400', view: 'reports' },
        { label: 'Agentes IA', icon: Bot, count: '11', subtitle: 'Activos', color: 'text-orange-600 dark:text-orange-400', view: 'agents' },
        { label: 'Gráficas', icon: BarChart3, count: '—', subtitle: 'Visualizaciones', color: 'text-teal-600 dark:text-teal-400', view: 'graficas' },
      ];

      // Try to fetch counts from backend
      try {
        const [companies, metrics, docs, reports] = await Promise.allSettled([
          axios.get('/api/companies'),
          axios.get('/api/metrics'),
          axios.get('/api/documents'),
          axios.get('/api/reports'),
        ]);
        if (companies.status === 'fulfilled') results[2].count = companies.value.data?.length ?? '—';
        if (metrics.status === 'fulfilled') results[3].count = metrics.value.data?.length ?? '—';
        if (docs.status === 'fulfilled') results[4].count = docs.value.data?.length ?? '—';
        if (reports.status === 'fulfilled') results[5].count = reports.value.data?.length ?? '—';
      } catch { /* ignore — shows dashes */ }

      // Check if LLM config available for fund count
      try {
        const res = await fetch('/api/llm/config');
        if (res.ok) {
          const data = await res.json();
          results[0].count = data.cache?.funds ?? '—';
          results[0].subtitle = data.cache?.fresh ? 'En caché' : 'Sin cargar';
        }
      } catch { /* ignore */ }

      setSections(results);
      setLoading(false);
    }
    loadStatus();
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Panel de Control</h1>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
          Estado global de la plataforma economIA
        </p>
      </div>

      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-28 bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700/50 animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {sections.map((s) => (
            <button
              key={s.label}
              onClick={() => onNavigate(s.view)}
              className="text-left bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700/50 p-5 hover:shadow-md hover:border-blue-300 dark:hover:border-blue-700 transition-all group"
            >
              <div className="flex items-center justify-between mb-3">
                <s.icon className={clsx('h-5 w-5', s.color)} />
                <span className="text-xs text-gray-400 dark:text-gray-500 group-hover:text-blue-500 transition-colors">→</span>
              </div>
              <p className="text-2xl font-bold text-gray-900 dark:text-gray-100">{s.count}</p>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{s.label} · {s.subtitle}</p>
            </button>
          ))}
        </div>
      )}

      {/* Quick actions */}
      <section className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700/50 p-5">
        <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
          <Activity className="h-4 w-4" /> Accesos rápidos
        </h3>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {[
            { label: 'Cargar fondos', view: 'global', desc: 'Ranking LLM' },
            { label: 'Chat IA', view: 'chat', desc: 'Consultas' },
            { label: 'Screener', view: 'screener', desc: 'Buscar' },
            { label: 'Briefing', view: 'briefing', desc: 'Noticias' },
          ].map((a) => (
            <button
              key={a.view}
              onClick={() => onNavigate(a.view)}
              className="px-4 py-3 rounded-lg bg-gray-50 dark:bg-gray-800 hover:bg-blue-50 dark:hover:bg-blue-900/20 border border-gray-200 dark:border-gray-700 text-left transition-colors"
            >
              <p className="text-sm font-medium text-gray-800 dark:text-gray-200">{a.label}</p>
              <p className="text-xs text-gray-400 dark:text-gray-500">{a.desc}</p>
            </button>
          ))}
        </div>
      </section>
    </div>
  );
}
