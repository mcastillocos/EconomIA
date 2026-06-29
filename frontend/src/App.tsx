import { useSignalR } from './hooks/useSignalR';
import { useStreamFunds } from './hooks/useFunds';
import { useTheme } from './hooks/useTheme';
import { useEffect, useCallback, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import Header from './components/Layout/Header';
import Sidebar from './components/Layout/Sidebar';
import type { ViewId } from './components/Layout/Sidebar';
import { GlobalView, DatosView, GraficasView } from './components/Views/Views';
import { OverviewView } from './components/Views/OverviewView';
import { LogsView } from './components/Views/LogsView';
import { MisFondosView } from './components/Views/MisFondosView';
import { ConfigView } from './components/Views/ConfigView';
import { StatsView } from './components/Views/StatsView';
import { CompaniesView } from './components/Views/CompaniesView';
import { WatchlistsView } from './components/Views/WatchlistsView';
import { UploadsView } from './components/Views/UploadsView';
import { MetricsView } from './components/Views/MetricsView';
import { ReportsView } from './components/Views/ReportsView';
import { BriefingView } from './components/Views/BriefingView';
import { ScreenerView } from './components/Views/ScreenerView';
import { ChatView } from './components/Views/ChatView';
import { AgentsView } from './components/Views/AgentsView';
import NewsView from './components/Views/NewsView';
import { appLog } from './store/logStore';
import { getViewFromPath, getPathFromView } from './config/routes';

const FUND_VIEWS: ViewId[] = ['global', 'datos', 'graficas', 'misfondos'];

function App() {
  useSignalR();
  const { isDark, toggle } = useTheme();
  const { funds, isStreaming, workersCompleted, workersTotal, startStream } = useStreamFunds();
  const location = useLocation();
  const navigate = useNavigate();
  const activeView = getViewFromPath(location.pathname);
  const [fundsLoaded, setFundsLoaded] = useState(false);

  useEffect(() => {
    appLog.info('System', 'EconomIA Dashboard iniciado');
  }, []);

  // Only load funds when navigating to a fund view
  useEffect(() => {
    if (FUND_VIEWS.includes(activeView) && !fundsLoaded && !isStreaming) {
      startStream();
      setFundsLoaded(true);
    }
  }, [activeView, fundsLoaded, isStreaming, startStream]);

  // Reload handler resets fundsLoaded so a failed reload can be retried
  const handleReload = useCallback(() => {
    setFundsLoaded(false);
    startStream();
    setFundsLoaded(true);
  }, [startStream]);

  const handleChangeView = useCallback((view: ViewId | string) => {
    const path = getPathFromView(view as ViewId);
    navigate(path);
    appLog.debug('App', `Vista cambiada a: ${view}`);
  }, [navigate]);

  const isLoading = FUND_VIEWS.includes(activeView) && funds.length === 0 && isStreaming;
  const progress = workersTotal > 0 ? Math.round((workersCompleted / workersTotal) * 100) : 0;

  return (
    <div className="h-screen flex flex-col bg-gray-50 dark:bg-[#1c1c1c] transition-colors duration-300">
      <Header isDark={isDark} onToggleTheme={toggle} />

      {/* Progress bar during streaming */}
      {isStreaming && FUND_VIEWS.includes(activeView) && (
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
        <main className="flex-1 overflow-y-auto px-3 py-4 md:px-6 md:py-6 pb-20 md:pb-6">
          <div className="max-w-7xl mx-auto">
            {activeView === 'overview' && <OverviewView onNavigate={handleChangeView} />}
            {activeView === 'global' && <GlobalView funds={funds} isLoading={isLoading} />}
            {activeView === 'datos' && <DatosView funds={funds} isLoading={isLoading} />}
            {activeView === 'graficas' && <GraficasView funds={funds} />}
            {activeView === 'misfondos' && <MisFondosView topFunds={funds} />}
            {activeView === 'companies' && <CompaniesView />}
            {activeView === 'watchlists' && <WatchlistsView />}
            {activeView === 'uploads' && <UploadsView />}
            {activeView === 'metrics' && <MetricsView />}
            {activeView === 'reports' && <ReportsView />}
            {activeView === 'briefing' && <BriefingView />}
            {activeView === 'news' && <NewsView />}
            {activeView === 'screener' && <ScreenerView />}
            {activeView === 'chat' && <ChatView />}
            {activeView === 'agents' && <AgentsView />}
            {activeView === 'logs' && <LogsView />}
            {activeView === 'stats' && <StatsView />}
            {activeView === 'config' && <ConfigView onReload={handleReload} />}

            {/* Disclaimer visible */}
            <div className="mt-8 border-t border-gray-200 dark:border-gray-700 pt-4">
              <p className="text-[10px] text-gray-400 dark:text-gray-500 text-center">
                economIA es una herramienta de apoyo al análisis financiero. No constituye recomendación de inversión. Las decisiones finales son responsabilidad del usuario.
              </p>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}

export default App;
