using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The whole collectibles domain: the sets, the shop, minting, the Relics that exist and who holds
/// them, and the wearable avatars.
/// </summary>
/// <remarks>
/// One read for all of it because each half is meaningless alone -- a collection whose furniture
/// does not exist cannot be completed, an offer naming missing furniture takes emeralds and hands
/// over nothing, and an avatar nobody was given does not exist as far as the hotel is concerned.
/// This page is the only place any of that is visible before a player hits it.
/// </remarks>
public sealed record CollectiblesOverview(
    CollectiblesTotals Totals,
    IReadOnlyList<CollectionRow> Collections,
    IReadOnlyList<NftAvatarRow> NftAvatars,
    IReadOnlyList<NftStoreOfferRow> StoreOffers,
    IReadOnlyList<MintableTypeRow> MintableTypes,
    IReadOnlyList<MintTokenOfferRow> TokenOffers,
    IReadOnlyList<NftAssetRow> Assets,
    IReadOnlyList<NftClaimRow> Claims,
    IReadOnlyList<CollectorScore> TopCollectors
);
