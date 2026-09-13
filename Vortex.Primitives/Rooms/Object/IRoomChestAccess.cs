using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// What a wired box may take out of a chest standing in the same room.
/// </summary>
/// <remarks>
/// In-process, like the other room accesses: the logic runs inside the room's own activation, so it
/// reaches the chest through this rather than through a grain call that would re-enter the
/// activation it is already on.
/// </remarks>
public interface IRoomChestAccess
{
    Task<int> PayOutChestCreditsAsync(
        int chestId,
        PlayerId playerId,
        int amount,
        bool everything,
        CancellationToken ct
    );

    Task<int> PayOutChestItemsAsync(
        int chestId,
        PlayerId playerId,
        int count,
        CancellationToken ct
    );

    /// <summary>
    /// How many items the given chests hold between them.
    /// </summary>
    /// <remarks>
    /// <paramref name="kindExampleItemIds"/> names furni standing in the room whose kind is the one
    /// being asked about — the client's "these item types" picker hands over examples, not type
    /// codes. An empty list counts everything the chests hold. Anything in
    /// <paramref name="chestIds"/> that is not a chest is ignored rather than counted as empty, so a
    /// box pointed at the wrong furni reads as "no chests" and not as "a chest with nothing in it".
    /// <para>
    /// Read through to the rows on every call. Chest contents are what the room's own payout actions
    /// move, and a condition answering from a warmed count would keep saying "there is stock" for as
    /// long as the entry lived — which is exactly the loop these boxes exist to close.
    /// </para>
    /// </remarks>
    Task<int> CountChestItemsAsync(
        IReadOnlyList<int> chestIds,
        IReadOnlyList<int> kindExampleItemIds,
        CancellationToken ct
    );
}
