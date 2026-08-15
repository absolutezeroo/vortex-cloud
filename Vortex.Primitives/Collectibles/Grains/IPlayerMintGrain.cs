using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Collectibles.Grains;

/// <summary>
/// One player's stamps and Relics: buying stamps, spending them on a conversion, and what the
/// conversion left behind.
/// </summary>
/// <remarks>
/// Keyed by player for the same reason the claims grain is: a conversion destroys a piece of
/// furniture and spends a balance, so two clicks on the confirm button must not run at once. A
/// player-keyed grain settles that without putting the whole hotel behind one lock.
/// </remarks>
public interface IPlayerMintGrain : IGrainWithIntegerKey
{
    /// <summary>How many stamps the player holds.</summary>
    public Task<int> GetTokenBalanceAsync(CancellationToken ct);

    /// <summary>
    /// Buys a stamp bundle with silver. Returns the balance afterwards, unchanged if the purchase
    /// was refused — the client re-reads the balance either way, so it always gets the truth.
    /// </summary>
    public Task<MintTokenPurchaseResult> PurchaseTokensAsync(int offerId, CancellationToken ct);

    /// <summary>
    /// Converts one item the player owns into a Relic: checks the terms, spends the stamps,
    /// destroys the furniture and records the asset.
    /// </summary>
    public Task<MintOutcome> MintAsync(int itemId, CancellationToken ct);

    /// <summary>The Relics the player holds, as the inventory's Collectibles tab lists them.</summary>
    public Task<ImmutableArray<CollectibleAssetSnapshot>> GetAssetsAsync(CancellationToken ct);
}

/// <summary>How a stamp purchase ended, and what the balance is now.</summary>
[GenerateSerializer, Immutable]
public sealed record MintTokenPurchaseResult
{
    [Id(0)]
    public required bool Purchased { get; init; }

    [Id(1)]
    public required int Balance { get; init; }
}

/// <summary>
/// How a conversion ended. The client tells success from failure and nothing more, but the reason
/// decides what is logged and is the only way to tell a refusal from a bug afterwards.
/// </summary>
public enum MintOutcome
{
    Minted = 0,
    MintingDisabled = 1,

    /// <summary>The item is not in this player's inventory — or not there any more.</summary>
    NotOwned = 2,

    /// <summary>Its classname is not on the mintable list, or its window has closed.</summary>
    NotMintable = 3,
    NotEnoughStamps = 4,
    Failed = 5,

    /// <summary>Every copy of a limited edition has already been converted.</summary>
    EditionExhausted = 6,
}
