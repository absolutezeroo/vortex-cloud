using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Collectibles.Grains;

/// <summary>
/// What this hotel lets players convert into Relics, and what stamps cost.
/// </summary>
/// <remarks>
/// A singleton caching admin-edited data, like <see cref="INftCollectionsGrain"/> and
/// <see cref="INftStoreGrain"/>. Nothing here is per-player: a mintable type is an offer standing
/// for everybody, and the conversion itself happens in <see cref="IPlayerMintGrain"/>.
/// </remarks>
public interface INftMintingGrain : IGrainWithStringKey
{
    /// <summary>Whether the hotel mints at all. False puts the whole minting half of the
    /// collectibles interface away, which is what an emulator with no mintable types wants.</summary>
    public Task<bool> IsMintingEnabledAsync(CancellationToken ct);

    /// <summary>What the minting tab lists. Disabled types and closed windows are already out.</summary>
    public Task<ImmutableArray<MintableItemTypeSnapshot>> GetMintableItemTypesAsync(
        CancellationToken ct
    );

    /// <summary>The stamp bundles the tab's dropdown offers.</summary>
    public Task<ImmutableArray<MintTokenOfferSnapshot>> GetTokenOffersAsync(CancellationToken ct);

    /// <summary>
    /// The bundle behind an offer id, or null if it is unknown or off sale. Read by the player's
    /// grain at purchase time so the price cannot be taken from anything the client sent.
    /// </summary>
    public Task<MintTokenOfferSnapshot?> FindTokenOfferAsync(int offerId, CancellationToken ct);

    /// <summary>
    /// What converting <paramref name="productCode"/> costs right now, or null when that furniture
    /// is not mintable — disabled, never listed, or outside its window.
    /// </summary>
    public Task<MintableTypeTerms?> FindMintableTermsAsync(
        string productCode,
        CancellationToken ct
    );

    /// <summary>Re-reads everything, so an admin's edits go live without a restart.</summary>
    public Task ReloadAsync(CancellationToken ct);
}

/// <summary>The terms a conversion is judged against, resolved server-side rather than trusted.</summary>
[GenerateSerializer, Immutable]
public sealed record MintableTypeTerms
{
    [Id(0)]
    public required string ProductCode { get; init; }

    [Id(1)]
    public required int StampPrice { get; init; }

    /// <summary>How many may ever exist. Zero is an open edition.</summary>
    [Id(2)]
    public required int EditionSize { get; init; }
}
