import { useSignalR } from './hooks/useSignalR';
import { useStreamFunds } from './hooks/useFunds';
import { useTheme } from './hooks/useTheme';
import { useEffect, useCallback, useState } from 'react';
import Header from './components/Layout/Header';
import Sidebar from './components/Layout/Sidebar';
import type { ViewId } from './components/Layout/Sidebar';
import { GlobalView, DatosView, GraficasView } from './components/Views/Views';
import { LogsView } from './components/Views/LogsView';
import { MisFondosView } from './components/Views/MisFondosView';
import { ConfigView } from './components/Views/ConfigView';
import { StatsView } from './components/Views/StatsView';
import { appLog } from './store/logStore';

function App() {
  useSignalR();
  const { isDark, toggle } = useTheme();
  const { funds, isStreaming, workersCompleted, workersTotal, startStream } = useStreamFunds();
  const [activeView, setActiveView] = useState<ViewId>('global');

  // Start SSE streaming on mount
  useEffect(() => {
    appLog.info('System', 'EconomIA Dashboard iniciado');
    startStream();
  }, [startStream]);

  const handleChangeView = useCallback((view: ViewId) => {
    setActiveView(view);
    appLog.debug('App', `Vista cambiada a: ${view}`);
  }, []);

  const isLoading = funds.length === 0 && isStreaming;
  const progress = workersTotal > 0 ? Math.round((workersCompleted / workersTotal) * 100) : 0;

  return (
    <div className="h-screen flex flex-col bg-gray-50 dark:bg-[#1c1c1c] transition-colors duration-300">
      <Header isDark={isDark} onToggleTheme={toggle} />

      {/* Progress bar during streaming */}
      {isStreaming && (
        <div className="px-6 pt-2 pb-1">
          <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
            <div className="flex-1 h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
              <div
                className="h-full bg-blue-500 rounded-full transition-all duration-300 ease-out"
                style={{ width: `${Math.max(progress, 5)}%` }}
              />
            </div>
            <span className="whitespace-nowrap font-mono">
              {workersCompleted}/{workersTotal} workers · {funds.length} fondos
            </span>
          </div>
        </div>
      )}

      <div className="flex flex-1 min-h-0">
        <Sidebar activeView={activeView} onChangeView={handleChangeView} />
        <main className="flex-1 overflow-y-auto px-6 py-6">
          <div className="max-w-7xl mx-auto">
            {activeView === 'global' && <GlobalView funds={funds} isLoading={isLoading} />}
            {activeView === 'datos' && <DatosView funds={funds} isLoading={isLoading} />}
            {activeView === 'graficas' && <GraficasView funds={funds} />}
            {activeView === 'misfondos' && <MisFondosView topFunds={funds} />}
            {activeView === 'logs' && <LogsView />}
            {activeView === 'stats' && <StatsView />}
            {activeView === 'config' && <ConfigView onReload={startStream} />}
          </div>
        </main>
      </div>
    </div>
  );
}

export default App;
