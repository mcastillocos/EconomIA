import { useState, useEffect } from 'react';
import { BarChart3, Building2, Bot, Database, FileText, Upload, TrendingUp, Activity, ClipboardCheck, Mic, Search, Newspaper, GitBranch, Sparkles } from 'lucide-react';
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
  const [recentActivity, setRecentActivity] = useState<{ label: string; time: string; icon: typeof BarChart3; color: string }[]>([]);

  useEffect(() => {
    async function loadStatus() {
      const results: SectionStatus[] = [
        { label: 'Fondos', icon: TrendingUp, count: '—', subtitle: 'Top ranking', color: 'text-blue-600 dark:text-blue-400', view: 'global' },
        { label: 'Empresas', icon: Building2, count: '—', subtitle: 'Registradas', color: 'text-green-600 dark:text-green-400', view: 'companies' },
        { label: 'Métricas', icon: Database, count: '—', subtitle: 'Datos cargados', color: 'text-amber-600 dark:text-amber-400', view: 'metrics' },
        { label: 'Documentos', icon: Upload, count: '—', subtitle: 'Subidos', color: 'text-indigo-600 dark:text-indigo-400', view: 'uploads' },
        { label: 'Informes', icon: FileText, count: '—', subtitle: 'Generados', color: 'text-pink-600 dark:text-pink-400', view: 'reports' },
        { label: 'Checklists', icon: ClipboardCheck, count: '—', subtitle: 'Instancias', color: 'text-violet-600 dark:text-violet-400', view: 'checklists' },
        { label: 'Earnings Calls', icon: Mic, count: '—', subtitle: 'Procesadas', color: 'text-purple-600 dark:text-purple-400', view: 'earnings' },
        { label: 'Recomendaciones', icon: Sparkles, count: '—', subtitle: 'Activas', color: 'text-rose-600 dark:text-rose-400', view: 'screener' },
      ];

      const activity: typeof recentActivity = [];

      try {
        const [companies, metrics, docs, reports, checklists, earnings, recs] = await Promise.allSettled([
          axios.get('/api/companies'),
          axios.get('/api/metrics'),
          axios.get('/api/documents'),
          axios.get('/api/reports'),
          axios.get('/api/checklists/instances'),
          axios.get('/api/earnings-calls'),
          axios.get('/api/screener/recommendations'),
        ]);
        if (companies.status === 'fulfilled') results[1].count = companies.value.data?.length ?? '—';
        if (metrics.status === 'fulfilled') results[2].count = metrics.value.data?.length ?? '—';
        if (docs.status === 'fulfilled') results[3].count = docs.value.data?.length ?? '—';
        if (reports.status === 'fulfilled') results[4].count = reports.value.data?.length ?? '—';
        if (checklists.status === 'fulfilled') {
          const instances = checklists.value.data;
          results[5].count = instances?.length ?? '—';
          const completed = instances?.filter((i: { status: string }) => i.status === 'completed')?.length ?? 0;
          if (completed > 0) results[5].subtitle = `${completed} completados`;
        }
        if (earnings.status === 'fulfilled') {
          const calls = earnings.value.data;
          results[6].count = calls?.length ?? '—';
          const analyzed = calls?.filter((c: { status: string }) => c.status === 'completed')?.length ?? 0;
          if (analyzed > 0) results[6].subtitle = `${analyzed} analizadas`;
          if (calls?.length > 0) {
            activity.push({ label: `Última call: ${calls[0].companyName}`, time: new Date(calls[0].createdAt).toLocaleDateString('es-ES'), icon: Mic, color: 'text-purple-500' });
          }
        }
        if (recs.status === 'fulfilled') {
          results[7].count = recs.value.data?.length ?? '—';
        }
      } catch { /* ignore */ }

      try {
        const res = await fetch('/api/llm/config');
        if (res.ok) {
          const data = await res.json();
          results[0].count = data.cache?.funds ?? '—';
          results[0].subtitle = data.cache?.fresh ? 'En caché' : 'Sin cargar';
        }
      } catch { /* ignore */ }

      setSections(results);
      setRecentActivity(activity);
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

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Quick actions */}
        <section className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700/50 p-5">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
            <Activity className="h-4 w-4" /> Accesos rápidos
          </h3>
          <div className="grid grid-cols-2 gap-3">
            {[
              { label: 'Chat IA', view: 'chat', desc: 'Consultas inteligentes', icon: Bot },
              { label: 'Screener', view: 'screener', desc: 'Recomendaciones personalizadas', icon: Search },
              { label: 'Earnings Call', view: 'earnings', desc: 'Subir audio/transcript', icon: Mic },
              { label: 'Checklists', view: 'checklists', desc: 'Due diligence', icon: ClipboardCheck },
              { label: 'Noticias', view: 'news', desc: 'Briefing diario', icon: Newspaper },
              { label: 'Workflows', view: 'workflows', desc: 'Automatizaciones', icon: GitBranch },
            ].map((a) => (
              <button
                key={a.view}
                onClick={() => onNavigate(a.view)}
                className="flex items-center gap-3 px-4 py-3 rounded-lg bg-gray-50 dark:bg-gray-800 hover:bg-blue-50 dark:hover:bg-blue-900/20 border border-gray-200 dark:border-gray-700 text-left transition-colors"
              >
                <a.icon className="h-4 w-4 text-gray-400" />
                <div>
                  <p className="text-sm font-medium text-gray-800 dark:text-gray-200">{a.label}</p>
                  <p className="text-xs text-gray-400 dark:text-gray-500">{a.desc}</p>
                </div>
              </button>
            ))}
          </div>
        </section>

        {/* Platform capabilities */}
        <section className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700/50 p-5">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
            <Sparkles className="h-4 w-4" /> Capacidades IA
          </h3>
          <div className="space-y-2">
            {[
              { label: 'Transcripción Whisper', desc: 'Audio → texto de earnings calls', status: 'activo' },
              { label: 'Análisis LLM', desc: 'Resumen, guidance, sentiment de calls', status: 'activo' },
              { label: 'Learning Screener', desc: 'Perfil inversor + recomendaciones proactivas', status: 'activo' },
              { label: 'Multi-Agent Workflows', desc: '11 agentes especializados encadenables', status: 'activo' },
              { label: 'Checklists Inteligentes', desc: '3 templates predefinidos para inversión', status: 'activo' },
              { label: 'Conectores Reales', desc: 'Tikr, Investing, FMP, RSS, PDF, Excel', status: 'activo' },
            ].map((cap) => (
              <div key={cap.label} className="flex items-center justify-between py-1.5 border-b border-gray-100 dark:border-gray-700/50 last:border-0">
                <div>
                  <p className="text-sm text-gray-700 dark:text-gray-300">{cap.label}</p>
                  <p className="text-xs text-gray-400">{cap.desc}</p>
                </div>
                <span className="text-xs px-2 py-0.5 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded-full">{cap.status}</span>
              </div>
            ))}
          </div>
        </section>
      </div>

      {recentActivity.length > 0 && (
        <section className="bg-white dark:bg-[#2a2a2a] rounded-xl border border-gray-200 dark:border-gray-700/50 p-5">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Actividad reciente</h3>
          <div className="space-y-2">
            {recentActivity.map((a, i) => (
              <div key={i} className="flex items-center gap-3 text-sm">
                <a.icon className={clsx('h-4 w-4', a.color)} />
                <span className="text-gray-700 dark:text-gray-300">{a.label}</span>
                <span className="text-xs text-gray-400 ml-auto">{a.time}</span>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
