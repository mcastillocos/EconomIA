import { TrendingUp, Moon, Sun, Users } from 'lucide-react';
import { APP_VERSION, GIT_HASH, BUILD_DATE } from 'virtual:app-meta';
import { usePresenceStore } from '../../store/presenceStore';

interface Props {
  isDark: boolean;
  onToggleTheme: () => void;
}

export default function Header({ isDark, onToggleTheme }: Props) {
  const onlineCount = usePresenceStore((s) => s.onlineCount);

  return (
    <header className="bg-white dark:bg-[#2a2a2a] border-b border-gray-200 dark:border-gray-700/50 shadow-sm transition-colors duration-300 flex-shrink-0">
      <div className="px-3 py-2 md:px-6 md:py-3 flex items-center justify-between">
        <div className="flex items-center gap-2 md:gap-3">
          <TrendingUp className="h-6 w-6 md:h-8 md:w-8 text-primary-600 dark:text-primary-400" />
          <h1 className="text-lg md:text-xl font-bold text-gray-900 dark:text-gray-100">
            Econom<span className="text-primary-600 dark:text-primary-400">IA</span>
          </h1>
          <span className="hidden sm:inline text-[10px] font-mono text-gray-400 dark:text-gray-500 bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded" title={`Build: ${BUILD_DATE} • Commit: ${GIT_HASH}`}>
            v{APP_VERSION}
          </span>
        </div>
        <div className="flex items-center gap-4">
          <span className="inline-flex items-center gap-1.5 text-sm text-green-700 dark:text-green-400 bg-green-50 dark:bg-green-900/30 px-3 py-1 rounded-full">
            <span className="h-2 w-2 rounded-full bg-green-500 animate-pulse"></span>
            En vivo
          </span>
          <span className="inline-flex items-center gap-1.5 text-sm text-blue-700 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/30 px-3 py-1 rounded-full" title={`${onlineCount} persona${onlineCount !== 1 ? 's' : ''} conectada${onlineCount !== 1 ? 's' : ''}`}>
            <Users className="h-3.5 w-3.5" />
            {onlineCount}
          </span>
          <button
            onClick={onToggleTheme}
            className="p-2 rounded-lg border border-gray-200 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
            aria-label="Toggle theme"
            title={isDark ? 'Cambiar a tema claro' : 'Cambiar a tema oscuro'}
          >
            {isDark ? (
              <Sun className="h-5 w-5 text-yellow-400" />
            ) : (
              <Moon className="h-5 w-5 text-gray-600" />
            )}
          </button>
        </div>
      </div>
    </header>
  );
}
