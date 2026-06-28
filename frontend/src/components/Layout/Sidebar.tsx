import { useState } from 'react';
import { LayoutDashboard, Table2, BarChart3, Briefcase, ScrollText, Settings, ChevronLeft, ChevronRight, Activity, Building2, ListChecks, Upload, Database, FileText, Newspaper, Search, MessageSquare, Bot } from 'lucide-react';
import clsx from 'clsx';

export type ViewId = 'global' | 'datos' | 'graficas' | 'misfondos' | 'companies' | 'watchlists' | 'uploads' | 'metrics' | 'reports' | 'briefing' | 'screener' | 'chat' | 'agents' | 'logs' | 'stats' | 'config';

interface NavItem {
  id: ViewId;
  label: string;
  icon: typeof LayoutDashboard;
  section?: string;
}

const navItems: NavItem[] = [
  { id: 'global', label: 'Global', icon: LayoutDashboard, section: 'Fondos' },
  { id: 'datos', label: 'Datos', icon: Table2 },
  { id: 'graficas', label: 'Gráficas', icon: BarChart3 },
  { id: 'misfondos', label: 'Mis Fondos', icon: Briefcase },
  { id: 'companies', label: 'Empresas', icon: Building2, section: 'Análisis' },
  { id: 'watchlists', label: 'Carteras', icon: ListChecks },
  { id: 'uploads', label: 'Uploads', icon: Upload },
  { id: 'metrics', label: 'Datos Norm.', icon: Database },
  { id: 'reports', label: 'Informes', icon: FileText, section: 'IA' },
  { id: 'briefing', label: 'Briefing', icon: Newspaper },
  { id: 'screener', label: 'Screener', icon: Search },
  { id: 'chat', label: 'Chat IA', icon: MessageSquare },
  { id: 'agents', label: 'Agentes', icon: Bot },
  { id: 'logs', label: 'Logs', icon: ScrollText, section: 'Sistema' },
  { id: 'stats', label: 'Estadísticas', icon: Activity },
  { id: 'config', label: 'Config', icon: Settings },
];

interface Props {
  activeView: ViewId;
  onChangeView: (view: ViewId) => void;
}

export default function Sidebar({ activeView, onChangeView }: Props) {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <>
      {/* Desktop sidebar */}
      <aside
        className={clsx(
          'hidden md:flex flex-col bg-white dark:bg-[#2a2a2a] border-r border-gray-200 dark:border-gray-700/50 transition-all duration-300 shadow-sm',
          collapsed ? 'w-16' : 'w-56'
        )}
      >
        <div className="flex items-center justify-end p-2 border-b border-gray-100 dark:border-gray-700/50">
          <button
            onClick={() => setCollapsed(!collapsed)}
            className="p-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-500 dark:text-gray-400"
            aria-label={collapsed ? 'Expandir menú' : 'Colapsar menú'}
          >
            {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
          </button>
        </div>
        <nav className="flex-1 py-4 px-2 space-y-1 overflow-y-auto">
          {navItems.map((item, idx) => {
            const isActive = activeView === item.id;
            const showSection = item.section && !collapsed && (idx === 0 || navItems[idx - 1]?.section !== item.section);
            return (
              <div key={item.id}>
                {showSection && (
                  <p className="text-[10px] uppercase font-semibold text-gray-400 dark:text-gray-500 px-3 pt-3 pb-1">{item.section}</p>
                )}
                <button
                  onClick={() => onChangeView(item.id)}
                  className={clsx(
                    'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors duration-200',
                    isActive
                      ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
                      : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700/50 hover:text-gray-900 dark:hover:text-gray-200'
                  )}
                  title={item.label}
                >
                  <item.icon className={clsx('h-5 w-5 flex-shrink-0', isActive ? 'text-primary-600 dark:text-primary-400' : '')} />
                  {!collapsed && <span className="truncate">{item.label}</span>}
                  {!collapsed && isActive && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-primary-500" />}
                </button>
              </div>
            );
          })}
        </nav>
        {!collapsed && (
          <div className="p-3 border-t border-gray-100 dark:border-gray-700/50">
            <p className="text-xs text-gray-400 dark:text-gray-500 text-center">Navegación</p>
          </div>
        )}
      </aside>

      {/* Mobile bottom nav */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 z-50 bg-white dark:bg-[#2a2a2a] border-t border-gray-200 dark:border-gray-700/50 shadow-lg">
        <div className="flex justify-around items-center px-1 py-1 overflow-x-auto">
          {navItems.map((item) => {
            const isActive = activeView === item.id;
            return (
              <button
                key={item.id}
                onClick={() => onChangeView(item.id)}
                className={clsx(
                  'flex flex-col items-center gap-0.5 px-2 py-1.5 rounded-lg min-w-[3rem] text-[10px] font-medium transition-colors',
                  isActive
                    ? 'text-primary-600 dark:text-primary-400'
                    : 'text-gray-400 dark:text-gray-500'
                )}
              >
                <item.icon className={clsx('h-5 w-5', isActive ? 'text-primary-600 dark:text-primary-400' : '')} />
                <span className="truncate">{item.label}</span>
              </button>
            );
          })}
        </div>
      </nav>
    </>
  );
}
