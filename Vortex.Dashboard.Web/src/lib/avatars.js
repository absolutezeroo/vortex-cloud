import { writable, get } from 'svelte/store';
import { apiGet } from './api.js';
import { isPermissionDeniedError } from './permissions.js';

// Session cache of player id -> avatar-head URL (or null when the player has no figure / can't be
// resolved). Every place that shows a player (EntityLink) asks for its head through resolveAvatar();
// requests made within the same tick are batched into ONE /directory/avatars call, so a table of N
// player rows costs a single request, not N. Once resolved, an id is never re-fetched.
export const avatarCache = writable(new Map());

const pending = new Set();
let scheduled = false;
// Flip permanently once the endpoint denies us (operator lacks PlayersRead) — from then on we keep
// plain names and never ask again, instead of hammering the API with 403s.
let disabled = false;

async function flush() {
  scheduled = false;
  const ids = [...pending];
  pending.clear();
  if (ids.length === 0 || disabled) {
    return;
  }

  const next = new Map(get(avatarCache));
  try {
    const data = await apiGet(`/api/v1/directory/avatars?ids=${ids.join(',')}`);
    for (const item of data.items || []) {
      next.set(Number(item.id), item.avatarUrl || null);
    }
    // Any id the server didn't return (unknown player): cache null so we don't ask again.
    for (const id of ids) {
      if (!next.has(id)) next.set(id, null);
    }
  } catch (err) {
    if (isPermissionDeniedError(err)) {
      disabled = true;
    }
    // Mark this batch resolved-to-nothing so a transient error doesn't loop forever.
    for (const id of ids) next.set(id, null);
  }
  avatarCache.set(next);
}

export function resolveAvatar(id) {
  if (disabled || id === null || id === undefined || id === '') {
    return;
  }
  const numeric = Number(id);
  if (Number.isNaN(numeric) || get(avatarCache).has(numeric)) {
    return;
  }
  pending.add(numeric);
  if (!scheduled) {
    scheduled = true;
    setTimeout(flush, 0);
  }
}
