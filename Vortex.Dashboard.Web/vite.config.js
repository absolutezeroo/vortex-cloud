import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';

export default defineConfig({
  base: '/assets/',
  plugins: [tailwindcss(), svelte()],
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
});
