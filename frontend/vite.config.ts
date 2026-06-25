import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { llmApiPlugin } from './server/llmPlugin';

export default defineConfig({
  plugins: [react(), llmApiPlugin()],
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
      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,
      },
    },
  },
});
