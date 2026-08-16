using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Players.Grains;

/// <summary>
/// The avatar figure sets one player has unlocked by redeeming clothing furni.
/// </summary>
/// <remarks>
/// Keyed by player because redeeming is destructive — the furni is consumed — and the client leaves
/// its confirm button clickable until the answer arrives. A player-keyed grain is single-threaded,
/// which settles a double click without putting every account behind one lock.
/// </remarks>
public interface IPlayerClothingGrain : IGrainWithIntegerKey
{
    /// <summary>What the client is told at login, and again after every redemption.</summary>
    public Task<PlayerClothingSnapshot> GetUnlockedAsync(CancellationToken ct);

    /// <summary>
    /// Redeems one clothing furni the player owns: checks it grants something, consumes it, and
    /// records the sets. Returns the outcome together with the lists as they now stand, because the
    /// client's acknowledgement <em>is</em> that list arriving.
    /// </summary>
    public Task<ClothingRedeemResult> RedeemAsync(int itemId, CancellationToken ct);

    /// <summary>
    /// Of the sets in a look, those that must be owned and are not. Empty means the look is
    /// wearable.
    /// </summary>
    /// <remarks>
    /// Asked of one query rather than a cached list, because the answer needs both halves at once:
    /// which sets are sold, and which of those this player holds. A hotel that has not seeded the
    /// sellable list yet finds nothing and allows everything, which is the right way for this to be
    /// wrong.
    /// </remarks>
    public Task<ImmutableArray<int>> FindUnownedSellableAsync(
        ImmutableArray<int> figureSetIds,
        CancellationToken ct
    );
}

/// <summary>
/// The two lists the client keeps: the sets it may offer in the avatar editor, and the classnames
/// it should recognise as already bound.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PlayerClothingSnapshot
{
    [Id(0)]
    public required ImmutableArray<int> FigureSetIds { get; init; }

    [Id(1)]
    public required ImmutableArray<string> BoundFurnitureNames { get; init; }

    public static PlayerClothingSnapshot Empty { get; } =
        new() { FigureSetIds = [], BoundFurnitureNames = [] };
}

/// <summary>How a redemption ended, and the lists to send afterwards.</summary>
[GenerateSerializer, Immutable]
public sealed record ClothingRedeemResult
{
    [Id(0)]
    public required ClothingRedeemOutcome Outcome { get; init; }

    [Id(1)]
    public required PlayerClothingSnapshot Clothing { get; init; }
}

/// <summary>
/// Why a redemption did or did not happen. The client shows nothing either way — it waits for the
/// list and gives up silently after five seconds — so this exists for the log, which is the only
/// place a refusal can be seen at all.
/// </summary>
public enum ClothingRedeemOutcome
{
    Redeemed = 0,

    /// <summary>Not in this player's inventory, or not any more.</summary>
    NotOwned = 1,

    /// <summary>The furni grants no sets: nothing in the mapping names it.</summary>
    GrantsNothing = 2,

    /// <summary>Every set it grants was already unlocked; the furni is left alone.</summary>
    AlreadyOwned = 3,
    Failed = 4,
}
