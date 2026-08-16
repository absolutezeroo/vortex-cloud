// The dashboard's read cycle, backed by TanStack Query.
//
// This stays a wrapper rather than pages calling createQuery directly, because two of the four
// states a dashboard page shows are not things TanStack knows about: a read refused for want of a
// capability is not an error to apologise for (the page owns that branch and names the missing
// capability), and a timeout / dropped connection / 429 has to become the sentence describeApiError
// already writes rather than a raw code. Roughly forty pages still hand-roll this cycle; they should
// migrate onto one helper, not onto one library plus a copy of those two rules each.
//
// Usage in a page:
//   const rooms = createResource(() => ['rooms'], () => apiGet('/api/v1/rooms'));
//   $: items = rooms.data?.items ?? [];
//   {#if rooms.loading} ... {:else if rooms.forbidden} <AccessDeniedNotice ... /> {/if}
//
// The KEY is the important half and the part that is new. It is a function so it re-reads the
// page's filter variables, and it is the cache identity: two different pages of the same list are
// two entries, so paging back is instant instead of a round trip.
//
//   const list = createResource(() => ['bots', page, term], () => apiGet(`/api/v1/bots?...`));
//   function search() { page = 1; }   // no refresh() -- the key changed, so the read follows
//
// That is the behaviour change worth knowing about: moving a filter no longer needs a refresh()
// call, and neither does mounting. refresh() is now only for "something I wrote made this stale",
// and it invalidates the whole FAMILY (every key starting with the same first segment), because a
// create or a delete makes every page and every filter of that list stale, not just the one on
// screen.
//
// Reads that wait to be asked for -- a panel behind a tab, a row's detail -- pass `enabled`:
//   const detail = createResource(() => ['bot', selected], () => apiGet(`/api/v1/bots/${selected}`),
//     { enabled: () => selected !== null });
//
// It still pairs with createWriteOps -- hand it the refresh so a committed write re-reads:
//   const ops = createWriteOps(bots.refresh);
import { createQuery, useQueryClient, keepPreviousData } from '@tanstack/svelte-query';
import { describeApiError } from './api.js';
import { isPermissionDeniedError } from './permissions.js';

/** Reads stay fresh for half a minute; a dashboard operator is reading, not trading. */
const DEFAULT_STALE_TIME_MS = 30_000;

/**
 * @param key () => unknown[] -- the cache identity, re-read whenever the page's filters change.
 * @param loader () => Promise<T> -- the actual request.
 * @param options.enabled () => boolean -- false holds the read back entirely.
 * @param options.staleTime how long a cached read is served without a refetch.
 */
export function createResource(key, loader, options = {}) {
  const client = useQueryClient();

  const query = createQuery(() => ({
    queryKey: key(),
    queryFn: loader,
    enabled: options.enabled ? options.enabled() : true,
    staleTime: options.staleTime ?? DEFAULT_STALE_TIME_MS,
    // Keep the rows on screen while the next page travels, so changing a filter does not blank the
    // table and jump the scroll. This is what the hand-written version did by never clearing on the
    // way out, and it is why `loading` below maps to isFetching rather than isPending.
    placeholderData: keepPreviousData,
    // Retrying a 403 is pointless -- the session will not gain the capability on the second attempt
    // -- and it delays the notice the operator needs to read.
    retry: (failureCount, error) => !isPermissionDeniedError(error) && failureCount < 2,
  }));

  // Getters rather than a snapshot: each read happens inside the caller's render, so it registers
  // as a dependency there. That is also why this file needs no runes of its own.
  return {
    /** Cleared on failure, so a page never draws stale rows underneath an error it just reported. */
    get data() {
      return query.isError ? undefined : query.data;
    },
    get loading() {
      return query.isFetching;
    },
    get error() {
      if (!query.isError || isPermissionDeniedError(query.error)) return '';

      return describeApiError(query.error);
    },
    /** The read was refused for want of a capability; the page names which one. */
    get forbidden() {
      return query.isError && isPermissionDeniedError(query.error);
    },
    /** Marks the whole family stale -- see the note above on why it is the family and not this key. */
    refresh() {
      return client.invalidateQueries({ queryKey: key().slice(0, 1) });
    },
  };
}
