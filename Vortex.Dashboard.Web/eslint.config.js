// Narrow on purpose. This is not a style linter -- csharpier's counterpart for the front end does
// not exist here and formatting is not what hurts. It exists for one failure the build genuinely
// cannot see: an identifier referenced in a component's markup that nothing declares. Svelte
// compiles that to a global lookup, so `disabled={busy.createPoll}` after `busy` was refactored away
// builds clean, ships, and throws `ReferenceError: busy is not defined` the first time an operator
// opens the page. That is exactly how the polls page broke.
//
//   npm run lint
import js from '@eslint/js';
import svelte from 'eslint-plugin-svelte';
import globals from 'globals';

export default [
  js.configs.recommended,
  // `flat/base` is the parser wiring only. The plugin's `flat/recommended` rule set is deliberately
  // not used: several of its rules crash on ESLint 10 (no-reactive-functions calls an API that no
  // longer exists), and its style opinions are not what this config is for.
  ...svelte.configs['flat/base'],
  {
    languageOptions: {
      ecmaVersion: 2024,
      sourceType: 'module',
      globals: { ...globals.browser },
    },
    rules: {
      // The one that matters.
      'no-undef': 'error',

      // Everything else is advisory here: the codebase predates the linter and a wall of style
      // complaints would just get the whole thing switched off. Turn one on when it starts earning
      // its keep.
      'no-unused-vars': ['warn', { args: 'none', caughtErrors: 'none' }],

      // Off: it does not understand Svelte reactivity. `prevOpen = open` at the end of a `$:` block
      // looks dead within that run but is exactly how the block remembers the previous value on the
      // next one, which is what makes "reset only on closed -> open" work.
      'no-useless-assignment': 'off',
    },
  },
  {
    ignores: ['node_modules/**', 'dist/**', '../Vortex.Dashboard.API/Assets/**'],
  },
];
