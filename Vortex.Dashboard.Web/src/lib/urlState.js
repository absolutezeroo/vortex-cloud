// Page state that belongs in the address bar: which tab is open, what the list is filtered to, which
// page of it you are on. Until this existed none of it was, so "the catalogue, page 3, Products tab"
// was somewhere you could only arrive by clicking -- not a link you could send to the person who
// needed to look at it, and not somewhere the back button could return you to.
//
//   import { readParam, writeParams } from '../lib/urlState.js';
//   let query = $state(readParam('q'));
//   writeParams({ q: query, page: page > 1 ? page : '' });   // '' removes the parameter
//
// svelte-spa-router keeps everything after the '?' of the hash route in its `querystring` store, so
// this is that store plus the write half.
import { querystring, location as routeLocation, replace } from 'svelte-spa-router';
import { get } from 'svelte/store';

/** Current value of one query parameter, or `fallback` when it is absent. */
export function readParam(name, fallback = '') {
  return new URLSearchParams(get(querystring) || '').get(name) ?? fallback;
}

/** Same, as a number -- for `page=` and other counters. Falls back when absent or not a number. */
export function readNumberParam(name, fallback = 0) {
  const parsed = Number(readParam(name, ''));
  return Number.isFinite(parsed) && parsed !== 0 ? parsed : fallback;
}

/**
 * Merge `patch` into the current query string. A value of '', null or undefined removes its
 * parameter, so the URL of an unfiltered list stays clean rather than carrying `?q=&page=1`.
 *
 * Uses replace, not push: filters are typed a character at a time, and one history entry per
 * keystroke turns the back button into an undo log nobody asked for.
 */
export function writeParams(patch) {
  const next = new URLSearchParams(get(querystring) || '');

  for (const [name, value] of Object.entries(patch)) {
    if (value === '' || value === null || value === undefined) {
      next.delete(name);
    } else {
      next.set(name, String(value));
    }
  }

  const qs = next.toString();
  const path = get(routeLocation);
  const target = qs ? `${path}?${qs}` : path;

  // Guard against re-entering the router with the URL it already has: writeParams runs from
  // $effect on several pages, and replacing the current location would re-run those effects.
  if (target !== `${path}${get(querystring) ? `?${get(querystring)}` : ''}`) {
    replace(target);
  }
}
