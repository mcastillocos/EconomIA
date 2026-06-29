import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import App from './App';
import './index.css';
import { appLog } from './store/logStore';

// ── Intercept console to capture logs ──────────────────────────────────────
const origConsole = {
  log: console.log,
  warn: console.warn,
  error: console.error,
};

console.log = (...args: unknown[]) => {
  origConsole.log(...args);
  const msg = args.map(String).join(' ');
  if (msg.startsWith('[EconomIA]') || msg.startsWith('[LLM]')) {
    appLog.info('System', msg);
  }
};

console.warn = (...args: unknown[]) => {
  origConsole.warn(...args);
  const msg = args.map(String).join(' ');
  appLog.warn('System', msg);
};

console.error = (...args: unknown[]) => {
  origConsole.error(...args);
  const msg = args.map(String).join(' ');
  // Skip React internal warnings about duplicate keys (already fixed)
  if (msg.includes('two children with the same key')) return;
  appLog.error('System', msg);
};

// ── Intercept fetch to log API calls ───────────────────────────────────────
const origFetch = window.fetch;
const failedEndpoints = new Set<string>();
window.fetch = async (input, init) => {
  const url = typeof input === 'string' ? input : input instanceof URL ? input.href : (input as Request).url;
  const method = init?.method ?? 'GET';
  const start = performance.now();

  // Only log our own API calls
  if (url.startsWith('/api/') || url.startsWith('http://localhost')) {
    appLog.debug('API', `${method} ${url}`);
  }

  try {
    const response = await origFetch(input, init);
    const elapsed = Math.round(performance.now() - start);

    if (url.startsWith('/api/') || url.startsWith('http://localhost')) {
      if (response.ok) {
        failedEndpoints.delete(url);
        appLog.info('API', `${method} ${url} → ${response.status} (${elapsed}ms)`);
      } else {
        appLog.warn('API', `${method} ${url} → ${response.status} (${elapsed}ms)`);
      }
    }

    return response;
  } catch (err) {
    if (url.startsWith('/api/') || url.startsWith('http://localhost')) {
      if ((err as Error).name !== 'AbortError') {
        // Only log first failure per endpoint to avoid spam
        if (!failedEndpoints.has(url)) {
          failedEndpoints.add(url);
          appLog.warn('API', `${method} ${url} → ${(err as Error).message} (backend no disponible)`);
        }
      }
    }
    throw err;
  }
};

// ── Global error handler ───────────────────────────────────────────────────
window.addEventListener('unhandledrejection', (event) => {
  appLog.error('System', `Unhandled rejection: ${event.reason}`);
});

window.addEventListener('error', (event) => {
  appLog.error('System', `Error: ${event.message} at ${event.filename}:${event.lineno}`);
});

// ── React Query ────────────────────────────────────────────────────────────

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      retryDelay: 500,
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <QueryClientProvider client={queryClient}>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </QueryClientProvider>
);
