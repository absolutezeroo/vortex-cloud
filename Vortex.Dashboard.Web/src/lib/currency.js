// Which currency an amount is in, and the chip colour that says so at a glance.
//
// An operator scanning fifty quests should not have to read "10 Duckets" / "10 Credits" word by
// word to see that two rows pay in different money. Colour carries that difference before the text
// is read -- so the chip is tinted by currency, not by the generic amber every price used to share.
//
// Colour is never the *only* carrier: every chip still spells the currency out. A wrong or unknown
// kind falls back to the neutral amber, so the worst case is a chip that looks like it always did.

/** The kinds we have a colour for. Anything else is `points`, which keeps the default amber. */
export const CURRENCY_KIND = {
  credits: 'credits',
  duckets: 'duckets',
  diamonds: 'diamonds',
  silver: 'silver',
  emeralds: 'emeralds',
  points: 'points',
};

/**
 * The reward encoding the quest wire uses, and the catalogue with it: a negative type means
 * credits, otherwise the number is an *activity point type*.
 *
 * 0 is duckets and 5 is diamonds -- documented in this repo's own catalogue data
 * (`tools/catalog_converter/input/catalog_items.sql`: "0 for duckets; 5 for diamonds; and any
 * seasonal/GOTW currencies"). Seasonal currencies take numbers of their own per hotel, which is
 * why anything else stays neutral rather than being guessed at.
 */
export function currencyKindFromRewardType(rewardType) {
  const type = Number(rewardType);

  if (!Number.isFinite(type)) return CURRENCY_KIND.points;
  if (type < 0) return CURRENCY_KIND.credits;
  if (type === 0) return CURRENCY_KIND.duckets;
  if (type === 5) return CURRENCY_KIND.diamonds;

  return CURRENCY_KIND.points;
}

/**
 * The wallet's own encoding, as `currency_types.type` stores it: 1 credits, 2 silver, 3 emeralds,
 * 4 activity points -- and only that last one needs `activityPointType` to say which.
 */
export function currencyKindFromType(currencyType, activityPointType = null) {
  switch (Number(currencyType)) {
    case 1:
      return CURRENCY_KIND.credits;
    case 2:
      return CURRENCY_KIND.silver;
    case 3:
      return CURRENCY_KIND.emeralds;
    case 4:
      return currencyKindFromRewardType(activityPointType ?? 0);
    default:
      return CURRENCY_KIND.points;
  }
}

/** The classes for a price/reward pill of this kind. Pair with `.cost-chip` in styles.css. */
export function currencyChipClass(kind) {
  return kind && kind !== CURRENCY_KIND.points
    ? `cost-chip cost-chip--${kind}`
    : 'cost-chip';
}
