import { defineConfig, type Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import { llmApiPlugin } from './server/llmPlugin';
import { execSync } from 'child_process';
import { readFileSync } from 'fs';

const pkg = JSON.parse(readFileSync('./package.json', 'utf-8'));
const gitHash = (() => {
  try { return execSync('git rev-parse --short HEAD').toString().trim(); }
  catch { return 'dev'; }
})();
const buildDate = new Date().toISOString().slice(0, 16).replace('T', ' ');

/**
 * Plugin que relee package.json en cada request (solo dev).
 * Así npm version:patch se refleja sin reiniciar Vite.
 */
function liveVersionPlugin(): Plugin {
  const virtualId = 'virtual:app-meta';
  const resolved = '\0' + virtualId;
  return {
    name: 'live-version',
    resolveId(id) {
      if (id === virtualId) return resolved;
    },
    load(id) {
      if (id !== resolved) return;
      const fresh = JSON.parse(readFileSync('./package.json', 'utf-8'));
      const hash = (() => {
        try { return execSync('git rev-parse --short HEAD').toString().trim(); }
        catch { return 'dev'; }
      })();
      return `export const APP_VERSION = ${JSON.stringify(fresh.version)};
export const GIT_HASH = ${JSON.stringify(hash)};
export const BUILD_DATE = ${JSON.stringify(new Date().toISOString().slice(0, 16).replace('T', ' '))};`;
    },
    configureServer(server) {
      server.watcher.add('./package.json');
      server.watcher.on('change', (file) => {
        if (file.replace(/\\/g, '/').endsWith('package.json')) {
          const mod = server.moduleGraph.getModuleById(resolved);
          if (mod) {
            server.moduleGraph.invalidateModule(mod);
            server.ws.send({ type: 'full-reload' });
          }
        }
      });
    },
  };
}

export default defineConfig({
  plugins: [react(), llmApiPlugin(), liveVersionPlugin()],
  define: {
    __APP_VERSION__: JSON.stringify(pkg.version),
    __GIT_HASH__: JSON.stringify(gitHash),
    __BUILD_DATE__: JSON.stringify(buildDate),
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
  server: {
    port: 3000,
    proxy: {
      '/api/funds': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/companies': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/watchlists': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/documents': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/metrics': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/chat': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/connectors': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/checklists': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/earnings-calls': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/screener': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/workflows': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/reports': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/export': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/alerts': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/api/portfoliooptimizer': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,
      },
    },
  },
});
