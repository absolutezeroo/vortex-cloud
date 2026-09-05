using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace Vortex.Primitives.RewardTracks;

/// <summary>
/// Which facts each action actually puts on its signal.
/// </summary>
/// <remarks>
/// <para>
/// This is the contract between the event handlers and the sequence editor, and it exists because
/// the alternative fails silently: an operator filtering <c>place_item</c> on <c>player</c> would
/// write a step that can never be satisfied, and nothing anywhere would say so. The dashboard reads
/// this to offer only the facts the chosen action emits, and the content validator reads it to
/// refuse the ones it does not.
/// </para>
/// <para>
/// It has to be kept in step with <c>RewardTrackEventHandlers</c> by hand — the handlers are the
/// truth, this is the description. <c>RewardTrackActionFactsTests</c> is what stops the two
/// drifting: it asserts every listed action exists and that the list and the handlers agree.
/// </para>
/// </remarks>
public static class RewardTrackActionFacts
{
    private static readonly string[] None = [];

    private static readonly FrozenDictionary<string, string[]> ByAction = new Dictionary<
        string,
        string[]
    >(System.StringComparer.Ordinal)
    {
        // RoomOwner is deliberately absent: PlayerEnteredRoomEvent does not carry it, and there is
        // no cheap way to read it on the entry path. Listing it would offer a filter that can never
        // match, which is the one thing this map exists to prevent.
        [RewardTrackActions.EnterOtherUsersRoom] = [RewardTrackFacts.Target, RewardTrackFacts.Room],
        [RewardTrackActions.CreateRoom] = [RewardTrackFacts.Room],
        [RewardTrackActions.PlaceItem] =
        [
            RewardTrackFacts.Target,
            RewardTrackFacts.Item,
            RewardTrackFacts.Definition,
            RewardTrackFacts.Placement,
            RewardTrackFacts.Room,
        ],
        [RewardTrackActions.MoveItem] = [RewardTrackFacts.Item, RewardTrackFacts.Room],
        [RewardTrackActions.RotateItem] = [RewardTrackFacts.Item, RewardTrackFacts.Room],
        [RewardTrackActions.PickUpItem] =
        [
            RewardTrackFacts.Target,
            RewardTrackFacts.Item,
            RewardTrackFacts.Room,
        ],
        [RewardTrackActions.WalkOnFurni] =
        [
            RewardTrackFacts.Target,
            RewardTrackFacts.Item,
            RewardTrackFacts.Definition,
            RewardTrackFacts.Room,
        ],
        [RewardTrackActions.ChatWithSomeone] = [RewardTrackFacts.Room],
        [RewardTrackActions.RequestFriend] = [RewardTrackFacts.Target, RewardTrackFacts.Player],
        [RewardTrackActions.SendMessengerMessage] =
        [
            RewardTrackFacts.Target,
            RewardTrackFacts.Player,
        ],
        [RewardTrackActions.GiveRespect] = [RewardTrackFacts.Target, RewardTrackFacts.Player],
        [RewardTrackActions.CompleteTrade] = [RewardTrackFacts.Player],
        [RewardTrackActions.BuyFromCatalogue] = [RewardTrackFacts.Target, RewardTrackFacts.Offer],
        [RewardTrackActions.UseHabbicon] =
        [
            RewardTrackFacts.Target,
            RewardTrackFacts.Habbicon,
            RewardTrackFacts.Room,
        ],
        [RewardTrackActions.CompleteHabbiconCollection] =
        [
            RewardTrackFacts.Target,
            RewardTrackFacts.Collection,
        ],
        [RewardTrackActions.PetLevel] = [RewardTrackFacts.Target, RewardTrackFacts.Pet],
        [RewardTrackActions.WearBadge] = [RewardTrackFacts.Target, RewardTrackFacts.Badge],
        [RewardTrackActions.Dance] = [RewardTrackFacts.Room],
        [RewardTrackActions.Wave] = [RewardTrackFacts.Room],
    }.ToFrozenDictionary(System.StringComparer.Ordinal);

    /// <summary>
    /// The facts this action emits. Empty for an action that carries none — those still work as a
    /// step, they just cannot be filtered or referenced.
    /// </summary>
    public static IReadOnlyList<string> For(string actionCode) =>
        ByAction.GetValueOrDefault(actionCode, None);

    /// <summary>Whether this action is known to emit this fact.</summary>
    public static bool Emits(string actionCode, string factKey) =>
        For(actionCode).Contains(factKey);
}
