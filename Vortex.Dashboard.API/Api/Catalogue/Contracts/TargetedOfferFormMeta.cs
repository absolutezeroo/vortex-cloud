using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// What the offer form needs that is not per-offer.
/// </summary>
/// <param name="ImageTemplate">The promo-image URL pattern, so the operator supplies a filename
/// with a live preview instead of a whole URL. Null when no asset root is configured, and the form
/// falls back to manual entry.</param>
/// <param name="CurrencyTypes">For the activity-point picker. Credits are filtered out client-side:
/// they already have their own price field.</param>
public sealed record TargetedOfferFormMeta(
    string? ImageTemplate,
    IReadOnlyList<TargetedOfferCurrency> CurrencyTypes
);
