using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Collectibles;

/// <summary>
/// Converting one piece of furniture the player owns into a Relic.
/// </summary>
/// <remarks>
/// The item is named by its <em>inventory</em> id, not by a classname: the minting tab picks the
/// first id it holds for the chosen type (<c>getIdsInInventory()[0]</c>) and sends that. So this
/// destroys one specific item, and which one is the client's choice rather than ours.
/// </remarks>
public record MintItemMessage : IMessageEvent
{
    public required int ItemId { get; init; }

    /// <summary>The wallet the Relic is credited to. Every player has exactly one here, so it is
    /// checked against theirs rather than trusted as an identity.</summary>
    public required string Wallet { get; init; }
}
