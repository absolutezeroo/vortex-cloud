// Small helpers for the hotel view editor. The formats themselves -- `a;b;c`, `k,v;k,v` -- are
// parsed and written on the server, deliberately: a reader here and a writer there is how the two
// drift. What is left for the browser is what the browser is for -- filling in a blank row, resolving
// a `${...}` so an operator can see the picture, saying in one line what a slot currently does.
import type {
  HotelViewCampaign,
  HotelViewElement,
  HotelViewElementType,
  HotelViewObject,
  HotelViewSlot,
  HotelViewVocabulary,
} from './apiTypes';

/**
 * A translator that falls back instead of printing its own key.
 *
 * Every name in this editor comes from the client's vocabulary, and the client can grow one we have
 * no string for. `$t` answers a missing key with the key, so without this a new element type would
 * render `hotelView.element.whatever` in the middle of the form. The fallback is the client's own
 * identifier, which is at least true.
 */
export const labeller =
  (translate: (key: string) => string) =>
  (key: string, fallback = ''): string => {
    const value = translate(key);
    return value === key ? fallback : value;
  };

/**
 * Turns a stored URL into one a browser can fetch.
 *
 * Backgrounds and promo art are written as `${image.library.url}reception/x.png`; the tokens are
 * ordinary external_variables keys the server resolved for us. An unresolved token is left as it is,
 * so a typo shows as the text the operator typed rather than as a broken image.
 */
export function resolveAssetUrl(
  uri: string | null | undefined,
  placeholders: Record<string, string>,
): string {
  if (!uri) return '';

  // A token's value can contain tokens -- `image.library.url` is `${url.prefix}/c_images/` in a
  // shipped dump -- so one pass leaves a hole and no picture. Bounded rather than looped to a fixed
  // point: two keys pointing at each other would otherwise hang the page.
  let out = uri;
  for (let pass = 0; pass < 5; pass += 1) {
    const next = out.replace(/\$\{([^}]+)\}/g, (whole, token: string) => placeholders[token] ?? whole);
    if (next === out) break;
    out = next;
  }
  return out;
}

/**
 * Where a background object's sprite really lives.
 *
 * `randomwalk` resolves `${image.library.url}<asset>.png` and the other three motions resolve
 * `${image.library.url}reception/<asset>.png`. Same asset string, two files: this is the rule that
 * makes switching an object's motion break its image with no error anywhere.
 */
export function objectAssetUrl(
  item: HotelViewObject,
  vocabulary: HotelViewVocabulary,
  placeholders: Record<string, string>,
): string {
  const motion = vocabulary.motionTypes.find((m) => m.motion === item.motion);
  if (!motion || !item.asset) return '';
  return resolveAssetUrl(`\${image.library.url}${motion.assetPrefix}${item.asset}.png`, placeholders);
}

/** Whether this widget reads its `conf` as a schedule rather than as a list of elements. */
export const isScheduled = (widget: string): boolean => widget === 'widgetcontainer';

/** A new element of the given type, with one empty slot per argument the client reads. */
export function blankElement(type: HotelViewElementType): HotelViewElement {
  return {
    type: type.type,
    args: type.arguments.map(() => ''),
    text: type.verified && type.arguments[0]?.kind === 'text' ? '' : null,
  };
}

/** The lowest free index, or 0 when all twenty are taken. */
export function freeObjectIndex(objects: HotelViewObject[], max: number): number {
  for (let index = 1; index <= max; index += 1) {
    if (!objects.some((item) => item.index === index)) return index;
  }
  return 0;
}

/** A new object on the given index, with one empty field per field its motion reads. */
export function blankObject(
  index: number,
  motion: string,
  vocabulary: HotelViewVocabulary,
): HotelViewObject {
  const shape = vocabulary.motionTypes.find((m) => m.motion === motion);
  return { index, asset: '', motion, fields: (shape?.fields ?? []).map(() => '') };
}

/**
 * Keeps an object's fields aligned with its motion when the motion changes.
 *
 * The fields are positional and the motions disagree about how many there are, so carrying the old
 * values across verbatim would silently reinterpret `driftX` as `centerX`. Shared leading fields
 * survive by position -- the first four are a start point and a speed under three of the four
 * motions -- and the rest is cleared.
 */
export function refitObject(
  item: HotelViewObject,
  motion: string,
  vocabulary: HotelViewVocabulary,
): HotelViewObject {
  const shape = vocabulary.motionTypes.find((m) => m.motion === motion);
  const width = shape?.fields.length ?? 0;
  const kept = motion === 'animated' || item.motion === 'animated' ? 0 : 4;

  return {
    ...item,
    motion,
    fields: Array.from({ length: width }, (_, i) => (i < kept ? (item.fields[i] ?? '') : '')),
  };
}

/** One line saying what a slot currently does, for the grid and for the audit summary. */
export function describeSlot(slot: HotelViewSlot | undefined, empty: string): string {
  if (!slot || !slot.widget) return empty;
  if (isScheduled(slot.widget)) return `${slot.widget} · ${slot.schedule.length}`;
  return slot.elements.length > 0 ? `${slot.widget} · ${slot.elements.length}` : slot.widget;
}

/** One line saying what a campaign holds. */
export const describeCampaign = (campaign: HotelViewCampaign): string =>
  `${campaign.widget || '—'} · ${campaign.elements.length}`;

/** Every campaign code any slot's schedule names, including ones no campaign defines. */
export function scheduledCodes(slots: HotelViewSlot[]): string[] {
  const codes = new Set<string>();
  for (const slot of slots) {
    for (const entry of slot.schedule) {
      if (entry.code) codes.add(entry.code);
    }
  }
  return [...codes].sort();
}
