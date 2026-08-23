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
export function filterRows(rows, term, keys = null) {
  const words = String(term || '')
    .toLowerCase()
    .split(/\s+/)
    .filter(Boolean);

  if (!words.length) return rows || [];

  return (rows || []).filter((row) => {
    const haystack = (keys ? keys.map((key) => row?.[key]) : Object.values(row ?? {}))
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
export function sortRows(rows, sort) {
  if (!sort?.key) return rows || [];

  const factor = sort.dir === 'asc' ? 1 : -1;

  return [...(rows || [])].sort((left, right) => {
    const a = left?.[sort.key];
    const b = right?.[sort.key];

    if (a === b) return 0;
    if (a === null || a === undefined) return 1;
    if (b === null || b === undefined) return -1;

    const bothNumeric = typeof a === 'number' && typeof b === 'number';

    return factor * (bothNumeric ? a - b : String(a).localeCompare(String(b), undefined, { numeric: true }));
  });
}

/** Header click: same column flips direction, a new column starts on the one people expect. */
export function toggleSort(sort, key, initialDir = 'desc') {
  if (sort.key !== key) return { key, dir: initialDir };
  if (sort.dir === initialDir) return { key, dir: initialDir === 'desc' ? 'asc' : 'desc' };
  return { key: '', dir: initialDir };
}
