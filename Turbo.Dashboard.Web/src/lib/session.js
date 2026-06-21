// Shared dashboard session state. The authenticated identity, the entity-inspector modal and the
// last access-denied route live here so components can consume them directly instead of threading
// props through the page tree (svelte-spa-router mounts pages without an intermediate parent).

import { writable } from 'svelte/store';

/** The authenticated principal from /api/me ({ email, superuser, capabilities }) or null. */
export const identity = writable(null);

/** Current emulator/API reachability issue, or null when the backend is reachable. */
export const connectionIssue = writable(null);

/** The currently open entity inspector ({ type, id, label }) or null when closed. */
export const modal = writable(null);

/** The route a user was denied access to, surfaced by the /access-denied view. */
export const deniedRoute = writable('');

export function openPlayer(id, label = '') {
  if (id === null || id === undefined || id === '') {
    return;
  }

  modal.set({ type: 'player', id, label: label || `player #${id}` });
}

export function openItem(id) {
  if (id === null || id === undefined || id === '') {
    return;
  }

  modal.set({ type: 'item', id, label: `item #${id}` });
}

export function closeModal() {
  modal.set(null);
}
