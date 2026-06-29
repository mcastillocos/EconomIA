import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { LayoutDashboard, Table2, BarChart3, Briefcase, ScrollText, Settings, ChevronLeft, ChevronRight, ChevronDown, ChevronUp, Activity, Building2, ListChecks, Upload, Database, FileText, Newspaper, Search, MessageSquare, Bot, Home, Rss, GitBranch, ClipboardCheck, Mic } from 'lucide-react';
import clsx from 'clsx';
import { getViewFromPath, getPathFromView } from '../../config/routes';

export type ViewId = 'overview' | 'global' | 'datos' | 'graficas' | 'misfondos' | 'companies' | 'watchlists' | 'uploads' | 'metrics' | 'reports' | 'briefing' | 'news' | 'screener' | 'chat' | 'agents' | 'workflows' | 'checklists' | 'earnings' | 'logs' | 'stats' | 'config';

interface NavItem {
  id: ViewId;
  label: string;
  icon: typeof LayoutDashboard;
  section: string;
}

const NAV_SECTIONS = ['Principal', 'Fondos', 'Análisis', 'IA', 'Sistema'] as const;

const navItems: NavItem[] = [
  { id: 'overview', label: 'Panel', icon: Home, section: 'Principal' },
  { id: 'global', label: 'Global', icon: LayoutDashboard, section: 'Fondos' },
  { id: 'datos', label: 'Datos', icon: Table2, section: 'Fondos' },
  { id: 'graficas', label: 'Gráficas', icon: BarChart3, section: 'Fondos' },
  { id: 'misfondos', label: 'Mis Fondos', icon: Briefcase, section: 'Fondos' },
  { id: 'companies', label: 'Empresas', icon: Building2, section: 'Análisis' },
  { id: 'watchlists', label: 'Carteras', icon: ListChecks, section: 'Análisis' },
  { id: 'uploads', label: 'Subidas', icon: Upload, section: 'Análisis' },
  { id: 'metrics', label: 'Datos Norm.', icon: Database, section: 'Análisis' },
  { id: 'reports', label: 'Informes', icon: FileText, section: 'IA' },
  { id: 'briefing', label: 'Briefing', icon: Newspaper, section: 'IA' },
  { id: 'news', label: 'Noticias', icon: Rss, section: 'IA' },
  { id: 'screener', label: 'Buscador', icon: Search, section: 'IA' },
  { id: 'chat', label: 'Chat IA', icon: MessageSquare, section: 'IA' },
  { id: 'agents', label: 'Agentes', icon: Bot, section: 'IA' },
  { id: 'workflows', label: 'Flujos', icon: GitBranch, section: 'IA' },
  { id: 'checklists', label: 'Checklists', icon: ClipboardCheck, section: 'IA' },
  { id: 'earnings', label: 'Llamadas', icon: Mic, section: 'IA' },
  { id: 'logs', label: 'Registros', icon: ScrollText, section: 'Sistema' },
  { id: 'stats', label: 'Estadísticas', icon: Activity, section: 'Sistema' },
  { id: 'config', label: 'Configuración', icon: Settings, section: 'Sistema' },
];

interface Props {
  activeView: ViewId;
  onChangeView: (view: ViewId) => void;
}

export default function Sidebar({ onChangeView }: Props) {
  const [collapsed, setCollapsed] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();

  // Sync with URL - use URL as source of truth for active state
  const currentView = getViewFromPath(location.pathname);

  const handleNav = (id: ViewId) => {
    navigate(getPathFromView(id));
    onChangeView(id);
  };

  const [collapsedSections, setCollapsedSections] = useState<Record<string, boolean>>({
    'Fondos': true,
    'Análisis': true,
    'IA': true,
    'Sistema': true,
  });

  const toggleSection = (section: string) => {
    setCollapsedSections(prev => ({ ...prev, [section]: !prev[section] }));
  };

  // Group items by section
  const grouped = NAV_SECTIONS.map(section => ({
    name: section,
    items: navItems.filter(i => i.section === section),
  }));

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
        <nav className="flex-1 py-2 px-2 space-y-0.5 overflow-y-auto">
          {grouped.map(({ name, items }) => {
            const isSectionCollapsed = collapsedSections[name] ?? false;
            const sectionHasActive = items.some(i => i.id === currentView);

            return (
              <div key={name}>
                {/* Section header */}
                {!collapsed && (
                  <button
                    onClick={() => toggleSection(name)}
                    className="w-full flex items-center justify-between px-3 pt-3 pb-1 group"
                  >
                    <span className="text-[10px] uppercase font-semibold text-gray-400 dark:text-gray-500 group-hover:text-gray-600 dark:group-hover:text-gray-300 transition-colors">
                      {name}
                      {sectionHasActive && isSectionCollapsed && <span className="ml-1 text-primary-500">•</span>}
                    </span>
                    {isSectionCollapsed
                      ? <ChevronDown className="h-3 w-3 text-gray-400 dark:text-gray-500" />
                      : <ChevronUp className="h-3 w-3 text-gray-400 dark:text-gray-500" />}
                  </button>
                )}

                {/* Section items */}
                {(!isSectionCollapsed || collapsed) && items.map((item) => {
                  const isActive = currentView === item.id;
                  return (
                    <button
                      key={item.id}
                      onClick={() => handleNav(item.id)}
                      className={clsx(
                        'w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors duration-200',
                        isActive
                          ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
                          : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700/50 hover:text-gray-900 dark:hover:text-gray-200'
                      )}
                      title={item.label}
                    >
                      <item.icon className={clsx('h-4.5 w-4.5 flex-shrink-0', isActive ? 'text-primary-600 dark:text-primary-400' : '')} />
                      {!collapsed && <span className="truncate">{item.label}</span>}
                      {!collapsed && isActive && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-primary-500" />}
                    </button>
                  );
                })}
              </div>
            );
          })}
        </nav>
      </aside>

      {/* Mobile bottom nav — show only key items */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 z-50 bg-white dark:bg-[#2a2a2a] border-t border-gray-200 dark:border-gray-700/50 shadow-lg">
        <div className="flex justify-around items-center px-1 py-1">
          {navItems.filter(i => ['overview', 'global', 'chat', 'agents', 'config'].includes(i.id)).map((item) => {
            const isActive = currentView === item.id;
            return (
              <button
                key={item.id}
                onClick={() => handleNav(item.id)}
                className={clsx(
                  'flex flex-col items-center gap-0.5 px-3 py-1.5 rounded-lg text-[10px] font-medium transition-colors',
                  isActive
                    ? 'text-primary-600 dark:text-primary-400'
                    : 'text-gray-400 dark:text-gray-500'
                )}
              >
                <item.icon className={clsx('h-5 w-5', isActive ? 'text-primary-600 dark:text-primary-400' : '')} />
                <span>{item.label}</span>
              </button>
            );
          })}
        </div>
      </nav>
    </>
  );
}
