using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Habbicons.Snapshots;

namespace Vortex.Primitives.Habbicons.Grains;

/// <summary>
/// One player's Habbicons: what they own, what they favourite, what they last used, and every way
/// those change. Keyed by player id, so Orleans' single-threaded turn is the whole of the
/// concurrency story — a double-click on "buy" is two turns, and the second one finds the first
/// one's row already there.
/// </summary>
public interface IPlayerHabbiconGrain : IGrainWithIntegerKey
{
    /// <summary>Owned rows plus recents, for the login push and for the hub.</summary>
    public Task<HabbiconInventorySnapshot> GetInventoryAsync(CancellationToken ct);

    /// <summary>The whole shop, resolved against this player's ownership.</summary>
    public Task<HabbiconShopSnapshot> GetShopAsync(CancellationToken ct);

    /// <summary>
    /// Pushes the owned list and the shop to the player. Called after login and after anything that
    /// changed either; the grain owns its own outbound, so no caller builds a composer.
    /// </summary>
    public Task PushInventoryAsync(CancellationToken ct);

    /// <summary>Answers the client's "tell me about this one" with a single shop row.</summary>
    public Task SendHabbiconInfoAsync(int habbiconId, CancellationToken ct);

    /// <summary>
    /// Grants a Habbicon outright, from any source. Idempotent: a second grant of something already
    /// owned reports <see cref="HabbiconGrantResult.WasNew"/> false and changes nothing, including
    /// when the player has favourited it.
    /// </summary>
    public Task<HabbiconGrantResult> GrantAsync(
        int habbiconId,
        HabbiconSource source,
        CancellationToken ct
    );

    /// <summary>Removes a Habbicon. Operator-only; returns false when the player did not own it.</summary>
    public Task<bool> RevokeAsync(int habbiconId, CancellationToken ct);

    /// <summary>Buys one Habbicon with the price on its definition.</summary>
    public Task BuyHabbiconAsync(int habbiconId, CancellationToken ct);

    /// <summary>Buys every entry of a collection the player is still missing, at the set price.</summary>
    public Task BuyCollectionAsync(int collectionId, CancellationToken ct);

    /// <summary>
    /// Claims a completed collection's bonus Habbicon. Refuses unless the collection is genuinely
    /// complete from stored ownership, and grants exactly once however many times it is called.
    /// </summary>
    public Task ClaimCollectionRewardAsync(int habbiconId, CancellationToken ct);

    /// <summary>Marks an owned Habbicon as a favourite (state 2 → 3).</summary>
    public Task SetFavouriteAsync(int habbiconId, bool favourite, CancellationToken ct);

    /// <summary>
    /// Uses a Habbicon in a room. Validates existence, ownership and a rate limit, then asks the
    /// room — which applies its own mute and flood gate — to show it, and publishes
    /// <c>HabbiconUsedEvent</c> only if it did.
    /// </summary>
    /// <param name="roomId">
    /// The room the session is in, from the server's own session state rather than the packet: the
    /// client sends nothing but a Habbicon id.
    /// </param>
    public Task UseInRoomAsync(int roomId, int habbiconId, CancellationToken ct);

    /// <summary>
    /// Uses a Habbicon inside a private conversation. Same validation, delivered through the
    /// messenger rather than the room.
    /// </summary>
    public Task UseInConversationAsync(
        int conversationPlayerId,
        int habbiconId,
        int confirmationId,
        CancellationToken ct
    );

    /// <summary>Whether the player owns a Habbicon, for anything that needs to ask without loading the list.</summary>
    public Task<bool> OwnsAsync(int habbiconId, CancellationToken ct);
}
