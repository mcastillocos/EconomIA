import { defineConfig } from 'vite';
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

export default defineConfig({
  plugins: [react(), llmApiPlugin()],
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
      '/api/reports': {
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
