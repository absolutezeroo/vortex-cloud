import { mount } from 'svelte';
import App from './App.svelte';
import './lib/theme.js';
import './styles.css';

mount(App, {
  target: document.getElementById('app'),
});
