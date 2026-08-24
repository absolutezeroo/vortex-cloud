import { mount } from 'svelte';
import App from './App.svelte';
import './lib/theme.js';
import './lib/i18n.js';
import './styles.css';

mount(App, {
  target: document.getElementById('app'),
});

// The in-page reporter: draw a box round what is wrong, say what you want, it lands in
// ui-reports.jsonl. Behind import.meta.env.DEV so the import is dropped from the bundle
// entirely -- the endpoint it posts to only exists in the dev server either way.
if (import.meta.env.DEV) {
  import('./lib/devReporter.js').then((m) => m.mountDevReporter());
}
