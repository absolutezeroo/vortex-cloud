// What a page declares when it wants to be filtered, and the two rules that apply to every filter
// on the site: an unset one is absent from the URL, and a set one is visible as a chip you can take
// off.
//
// Written because filtering was per-page furniture. Every list invented its own row of controls, so
// the pages nobody had looked at recently had one text box and the pages someone had needed had
// six -- and none of them survived a refresh or a shared link, because the values lived in
// component state and nowhere else.
//
//   const FIELDS: FilterField[] = [
//     { id: 'q', label: $t('rooms.search'), kind: 'text' },
//     { id: 'owner', label: $t('rooms.owner'), kind: 'entity', picker: 'user' },
//     { id: 'minPop', label: $t('rooms.minPopulation'), kind: 'number' },
//   ];
//
//   let values = $state(readFilterValues(FIELDS));
//   <FilterBar fields={FIELDS} bind:values />
//
// The page then reads `values.owner` -- in a resource key, or in a client-side predicate. FilterBar
// owns the controls, the URL and the chips; it owns nothing about what the values mean.
import { readParam, writeParams } from './urlState';

/** One control in the filter row. */
export type FilterField = {
  /** Also the query-parameter name, so it has to be URL-safe and stable. */
  id: string;
  label: string;
  /** Defaults to 'text'. */
  kind?: 'text' | 'number' | 'select' | 'date' | 'datetime' | 'bool' | 'entity';
  /** select: the choices. The empty value is added automatically as `anyLabel`. */
  options?: { value: string; label: string }[];
  /** entity: a key of DIRECTORIES ('user', 'room', 'furniture', 'group', ...). */
  picker?: string;
  /** select/entity: what "no value" reads as. Defaults to a shared "Any". */
  anyLabel?: string;
  placeholder?: string;
  /** Widen this control to two columns when its content needs the room. */
  wide?: boolean;
};

/** The values a filter row holds: field id to string, empty meaning unset. */
export type FilterValues = Record<string, string>;

/** Empty values for a set of fields — every id present, so a page can read one without a guard. */
export function emptyFilterValues(fields: FilterField[]): FilterValues {
  return Object.fromEntries(fields.map((field) => [field.id, '']));
}

/**
 * The values as the address bar has them, so a filtered list survives a refresh and can be sent to
 * whoever needs to look at it.
 */
export function readFilterValues(fields: FilterField[]): FilterValues {
  return Object.fromEntries(fields.map((field) => [field.id, readParam(field.id)]));
}

/** Writes the set ones and removes the rest, so an unfiltered list keeps a clean URL. */
export function writeFilterValues(values: FilterValues): void {
  writeParams(values);
}

/** Whether anything is filtered — what decides if the "clear" affordance is worth drawing. */
export const hasActiveFilters = (values: FilterValues): boolean =>
  Object.values(values).some((value) => value !== '');
