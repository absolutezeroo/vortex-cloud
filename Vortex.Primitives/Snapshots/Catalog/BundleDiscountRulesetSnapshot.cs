namespace Vortex.Primitives.Snapshots.Catalog;

public sealed record BundleDiscountRulesetSnapshot(
    int MaxPurchaseSize,
    int BundleSize,
    int BundleDiscountSize,
    int BonusThreshold,
    int[] AdditionalBonusDiscountThresholdQuantities
)
{
    /// <summary>
    ///     The ceiling the server hands the client for a single purchase, and the one the purchase
    ///     grain refuses above. It lives here because those two have to be the same number: the
    ///     client sizes its quantity selector from what this ruleset advertises, so a server that
    ///     accepted more than it advertised would be enforcing nothing, and one that accepted less
    ///     would reject purchases its own UI offered.
    /// </summary>
    public const int DEFAULT_MAX_PURCHASE_SIZE = 100;
}
