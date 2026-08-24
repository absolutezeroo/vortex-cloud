import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { uiReports } from './tools/vite-plugin-ui-reports.js';

// `npm run dev` = HMR against the running emulator: Vite serves the SPA, /api is proxied to the
// dashboard host so the session cookie stays same-origin. The build still emits under /assets/,
// which is where DashboardEndpoints.MapFrontend serves the embedded copy from.
export default defineConfig(({ command }) => ({
  base: command === 'build' ? '/assets/' : '/',
  plugins: [tailwindcss(), svelte(), uiReports()],
  server: {
    port: 9001,
    strictPort: true,
    proxy: { '/api': 'http://localhost:9000' },
  },
  build: {
    outDir: '../Vortex.Dashboard.API/Assets',
    emptyOutDir: true,
    assetsDir: '',
    rollupOptions: {
      output: {
        entryFileNames: 'dashboard-[hash].js',
        assetFileNames: 'dashboard-[hash][extname]',
      },
    },
  },
}));
