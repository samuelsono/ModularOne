import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    // Module boundary aliases (ADR 0001). Target folders are populated as
    // features migrate into src/platform and src/modules in later phases.
    alias: {
      '@platform': fileURLToPath(new URL('./src/platform', import.meta.url)),
      '@modules': fileURLToPath(new URL('./src/modules', import.meta.url)),
    },
  },
  server: {
    proxy: {
      // Proxy API calls to the app service
      '/api': {
        target: process.env.SERVER_HTTPS || process.env.SERVER_HTTP,
        changeOrigin: true
      },
      '/hubs': {
        target: process.env.SERVER_HTTPS || process.env.SERVER_HTTP,
        changeOrigin: true,
        ws: true,
      },
    }
  }
});
