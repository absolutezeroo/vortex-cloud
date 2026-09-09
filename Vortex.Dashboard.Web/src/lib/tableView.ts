// Client-side filtering and sorting for the tables that are already fully loaded in the page.
//
// These are the stats and queue tables -- one request brings the whole list, and the operator then
// has to find one row in it by eye. The lists that page against the server (catalogue, audit,
// furniture) keep doing that; this is only for the ones that do not.
//
//   let q = $state('');
//   let sort = $state({ key: 'count', dir: 'desc' });
//   let view = $derived(sortRows(filterRows(rows, q), sort));

/**
 * Rows whose text contains every whitespace-separated word of `term`, in any field.
 *
 * Every field rather than a named list: an operator searching a stats table types whatever they
 * remember -- a logic key, a room name, an id -- and having to declare per table which columns are
 * searchable is how a column ends up quietly unsearchable.
 */
/**
 * A table row: whatever the endpoint answered, read by key.
 *
 * `object` rather than `Record<string, unknown>` on purpose. The rows are now generated response
 * records, which have no index signature, and requiring one would mean either weakening every
 * contract or spreading each row at every call site. Reading a key off one is the job of
 * {@link field} below, which does the widening once.
 */
export type Row = object;

/** One column's value, whatever shape the row is. */
const field = (row: Row | null | undefined, key: string): unknown =>
  (row as Record<string, unknown> | null | undefined)?.[key];

/** Which column the table is ordered by, and which way. */
export type Sort = { key: string; dir?: 'asc' | 'desc' };

export function filterRows<T extends Row>(
  rows: T[] | null | undefined,
  term: string | null | undefined,
  keys: string[] | null = null,
): T[] {
  const words = String(term || '')
    .toLowerCase()
    .split(/\s+/)
    .filter(Boolean);

  if (!words.length) return rows || [];

  return (rows || []).filter((row) => {
    const haystack = (keys ? keys.map((key) => field(row, key)) : Object.values(row ?? {}))
      .filter((value) => value !== null && value !== undefined && typeof value !== 'object')
      .join(' ')
      .toLowerCase();

    return words.every((word) => haystack.includes(word));
  });
}

/**
 * A copy of `rows` ordered by `sort.key`. Numbers compare as numbers and everything else as text,
 * because a column of counts sorted lexically puts 9 after 10 and reads as a bug.
 *
 * `sort.key` empty means "leave the order the server chose" -- which is usually already the useful
 * one (most recent first, biggest first), so it is the default rather than something to switch off.
 */
export function sortRows<T extends Row>(
  rows: T[] | null | undefined,
  sort: Sort | null | undefined,
): T[] {
  if (!sort?.key) return rows || [];

  const factor = sort.dir === 'asc' ? 1 : -1;

  return [...(rows || [])].sort((left, right) => {
    const a = field(left, sort.key);
    const b = field(right, sort.key);

    if (a === b) return 0;
    if (a === null || a === undefined) return 1;
    if (b === null || b === undefined) return -1;

    const bothNumeric = typeof a === 'number' && typeof b === 'number';

    return factor * (bothNumeric ? a - b : String(a).localeCompare(String(b), undefined, { numeric: true }));
  });
}

/** Header click: same column flips direction, a new column starts on the one people expect. */
export function toggleSort(
  sort: Sort,
  key: string,
  initialDir: 'asc' | 'desc' = 'desc',
): Sort {
  if (sort.key !== key) return { key, dir: initialDir };
  if (sort.dir === initialDir) return { key, dir: initialDir === 'desc' ? 'asc' : 'desc' };
  return { key: '', dir: initialDir };
}

/**
 * How many rows of an already-loaded list to draw at once.
 *
 * These lists arrive in one response, so this is not about the request — it is about the table. A
 * queue with four hundred tickets rendered every one of them into the DOM and asked the operator to
 * scroll past the ones they were not looking for; the filters above narrow it, and this bounds
 * whatever is left.
 */
export const PAGE_SIZE = 50;

/** The `page`-th slice of `rows`, 1-based. Out-of-range pages clamp rather than answer nothing. */
export function pageOf<T>(rows: T[] | null | undefined, page: number, size = PAGE_SIZE): T[] {
  const all = rows || [];
  const last = pageCountOf(all, size);
  const current = Math.min(Math.max(1, page || 1), last);

  return all.slice((current - 1) * size, current * size);
}

/** How many pages `rows` makes — at least one, so an empty list is page 1 of 1 rather than of 0. */
export function pageCountOf(rows: unknown[] | null | undefined, size = PAGE_SIZE): number {
  return Math.max(1, Math.ceil((rows?.length ?? 0) / size));
}
