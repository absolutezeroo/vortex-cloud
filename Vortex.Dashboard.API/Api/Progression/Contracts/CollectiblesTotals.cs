namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The domain counted, with the "and how many of those actually work" figure beside each.
/// </summary>
/// <param name="UnresolvedItems">Collection items naming furniture that does not exist.</param>
/// <param name="StoreOffersOnSale">Enabled, not sold out, and resolvable -- the offers a player can
/// really buy, which is what the raw offer count does not say.</param>
/// <param name="MintableTypesOpen">Inside their window and resolvable.</param>
public sealed record CollectiblesTotals(
    int Collections,
    int Items,
    int UnresolvedItems,
    int CompletableCollections,
    int TrackedPlayers,
    int StoreOffers,
    int StoreOffersOnSale,
    int MintableTypes,
    int MintableTypesOpen,
    int MintedRelics,
    int StampsHeld,
    int NftAvatars,
    int NftAvatarsGranted
);
