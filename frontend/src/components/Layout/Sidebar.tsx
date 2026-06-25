import { useState } from 'react';
import { LayoutDashboard, Table2, BarChart3, Briefcase, ScrollText, Settings, ChevronLeft, ChevronRight, Activity } from 'lucide-react';
import clsx from 'clsx';

export type ViewId = 'global' | 'datos' | 'graficas' | 'misfondos' | 'logs' | 'stats' | 'config';

interface NavItem {
  id: ViewId;
  label: string;
  icon: typeof LayoutDashboard;
}

const navItems: NavItem[] = [
  { id: 'global', label: 'Global', icon: LayoutDashboard },
  { id: 'datos', label: 'Datos', icon: Table2 },
  { id: 'graficas', label: 'Gráficas', icon: BarChart3 },
  { id: 'misfondos', label: 'Mis Fondos', icon: Briefcase },
  { id: 'logs', label: 'Logs', icon: ScrollText },
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
    <aside
      className={clsx(
        'flex flex-col bg-white dark:bg-[#2a2a2a] border-r border-gray-200 dark:border-gray-700/50 transition-all duration-300 shadow-sm',
        collapsed ? 'w-16' : 'w-56'
      )}
    >
      {/* Collapse toggle */}
      <div className="flex items-center justify-end p-2 border-b border-gray-100 dark:border-gray-700/50">
        <button
          onClick={() => setCollapsed(!collapsed)}
          className="p-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-500 dark:text-gray-400"
          aria-label={collapsed ? 'Expandir menú' : 'Colapsar menú'}
        >
          {collapsed ? (
            <ChevronRight className="h-4 w-4" />
          ) : (
            <ChevronLeft className="h-4 w-4" />
          )}
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 py-4 px-2 space-y-1">
        {navItems.map((item) => {
          const isActive = activeView === item.id;
          return (
            <button
              key={item.id}
              onClick={() => onChangeView(item.id)}
              className={clsx(
                'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors duration-200',
                isActive
                  ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
                  : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700/50 hover:text-gray-900 dark:hover:text-gray-200'
              )}
              title={item.label}
            >
              <item.icon
                className={clsx(
                  'h-5 w-5 flex-shrink-0',
                  isActive ? 'text-primary-600 dark:text-primary-400' : ''
                )}
              />
              {!collapsed && (
                <span className="truncate">{item.label}</span>
              )}
              {!collapsed && isActive && (
                <span className="ml-auto h-1.5 w-1.5 rounded-full bg-primary-500" />
              )}
            </button>
          );
        })}
      </nav>

      {/* Footer hint */}
      {!collapsed && (
        <div className="p-3 border-t border-gray-100 dark:border-gray-700/50">
          <p className="text-xs text-gray-400 dark:text-gray-500 text-center">
            Navegación
          </p>
        </div>
      )}
    </aside>
  );
}
