// Transient notifications. The dashboard had none: every write reported itself inline, next to the
// form, which works while you are looking at the form and not at all when the write finishes after
// you have scrolled somewhere else.
//
//   import { toast } from '../lib/toasts.js';
//   toast.success('Voucher created');
//   toast.error(describeApiError(err), { timeout: 0 });   // 0 = stays until dismissed
//
// The host lives once in AppShell; nothing else needs to render it.
import { writable } from 'svelte/store';

export const toasts = writable([]);

let nextId = 1;

function push(kind, message, options = {}) {
  if (!message) return null;

  const id = nextId++;
  const timeout = options.timeout ?? (kind === 'error' ? 9000 : 5000);

  toasts.update((list) => [...list, { id, kind, message, title: options.title || '' }]);

  // 0 means "hold until dismissed" -- an error the operator has not read yet is not noise.
  if (timeout > 0) {
    setTimeout(() => dismiss(id), timeout);
  }

  return id;
}

export function dismiss(id) {
  toasts.update((list) => list.filter((entry) => entry.id !== id));
}

export const toast = {
  success: (message, options) => push('success', message, options),
  info: (message, options) => push('info', message, options),
  warning: (message, options) => push('warning', message, options),
  error: (message, options) => push('error', message, options),
};
