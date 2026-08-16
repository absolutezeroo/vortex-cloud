// Every page in this dashboard reads the same way: raise a loading flag, GET, sort the failure into
// "the session does not hold this capability" versus everything else, drop the data, lower the flag.
// It was written out by hand once per page -- `forbidden = true` appears in 41 files -- and every
// copy is a chance to forget one of the four states, to report a timeout as a bare HTTP code, or to
// let a slow response overwrite a newer one.
//
// createResource owns that cycle the way createWriteOps owns the write cycle, so a page describes
// what it reads instead of how reading works.
//
// Usage in a page:
//   const rooms = createResource(() => apiGet('/api/v1/rooms'));
//   $: items = $rooms.data?.items ?? [];
//   {#if $rooms.loading} ... {:else if $rooms.forbidden} <AccessDeniedNotice ... /> {/if}
//
// The loader is a closure, so it reads whatever the page's filter variables hold at the moment
// refresh() runs. A search box or a page number needs no wiring beyond calling refresh():
//   const list = createResource(() => apiGet(`/api/v1/bots?page=${page}`));
//   function search() { page = 1; void list.refresh(); }
//
// Several endpoints that belong to one screen are ONE resource, not three -- return them together
// and the page keeps a single loading flag and a single failure path, instead of a screen that is
// half-loaded and half-refused:
//   const data = createResource(async () => {
//     const [list, stats] = await Promise.all([apiGet('/api/v1/bots'), apiGet('/api/v1/bots/stats')]);
//     return { list, stats };
//   });
//
// It pairs with createWriteOps -- hand it the refresh so a committed write re-reads the page:
//   const ops = createWriteOps(data.refresh);
import { onMount } from 'svelte';
import { writable } from 'svelte/store';
import { describeApiError } from './api.js';
import { isPermissionDeniedError } from './permissions.js';

/**
 * @param loader called on every read; whatever it resolves to becomes `data`.
 * @param options.initial what `data` holds before the first read and after a failure (default null).
 * @param options.immediate set false for a resource that must not read until the page says so --
 *        a panel behind a tab, or a list that needs a row selected first.
 */
export function createResource(loader, options = {}) {
  const initial = options.initial ?? null;
  const state = writable({ data: initial, loading: false, error: '', forbidden: false });

  // Guards against out-of-order responses. Two refreshes in flight -- an operator retyping a search,
  // or a filter changed while the first read is still travelling -- and the *slower* one lands last
  // and wins, leaving the table showing results for a query that is no longer on screen. Every
  // hand-written copy of this cycle had that bug; none of them could have it consistently fixed
  // without this counter, because the page has nothing to compare a stale reply against.
  let sequence = 0;

  async function refresh() {
    const mine = ++sequence;

    // Deliberately does NOT clear `data`: keeping the previous rows on screen while the next read
    // travels is what stops every filter change from blanking the page and jumping the scroll.
    state.update((s) => ({ ...s, loading: true, error: '', forbidden: false }));

    try {
      const data = await loader();

      if (mine !== sequence) return false;

      state.update((s) => ({ ...s, data, loading: false }));
      return true;
    } catch (err) {
      if (mine !== sequence) return false;

      // A refused read is not an error to apologise for -- the page owns that branch and shows the
      // operator which capability is missing, so it gets its own flag rather than an error string.
      if (isPermissionDeniedError(err)) {
        state.update((s) => ({ ...s, data: initial, loading: false, forbidden: true }));
        return false;
      }

      // describeApiError, not err.message: a timeout, a dropped connection or a 429 each become a
      // sentence the operator can act on. The hand-written copies surfaced the raw code.
      state.update((s) => ({ ...s, data: initial, loading: false, error: describeApiError(err) }));
      return false;
    }
  }

  // Registered here rather than left to the page, which is why createResource must be called at the
  // top level of a component's <script>. A page that needs state prepared before the first read
  // (a default date window, say) should prepare it synchronously at init, not in its own onMount --
  // this callback is registered at the point createResource is called and would run first.
  if (options.immediate !== false) {
    onMount(() => {
      void refresh();
    });
  }

  return { subscribe: state.subscribe, refresh };
}
