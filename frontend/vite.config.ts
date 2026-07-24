/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';

// Dev proxy target for the API. Defaults to the local Docker stack (docker-compose.yml → :8090);
// override with VITE_API_PROXY when running the backend via `dotnet run` (e.g. http://localhost:5080).
const apiProxy = process.env.VITE_API_PROXY ?? 'http://localhost:8090';

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: apiProxy,
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    include: ['src/**/*.spec.ts'],
  },
});
