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

    // Vite refuses a request whose Host header it does not recognise — a DNS-rebinding guard — and a
    // tunnel arrives as `*.trycloudflare.com`, which it has never seen; without this the tunnelled
    // dashboard answers "Blocked request. This host is not allowed." and nothing else. Listed rather
    // than `true`: `true` is the guard switched off.
    //
    // This only affects the DEV server. The dashboard an operator actually reaches is the copy
    // embedded in the assembly on :9000, and exposing that one is a deployment decision, not a
    // config line here.
    allowedHosts: ['.trycloudflare.com', '.ngrok-free.app'],
    // `/hotel-assets` alongside `/api`: the image templates are relative now, so furni icons, badges
    // and promo art are asked for on THIS origin and the dev server has to forward them like the
    // rest.
    proxy: {
      '/api': 'http://localhost:9000',
      '/hotel-assets': 'http://localhost:9000',
    },
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
