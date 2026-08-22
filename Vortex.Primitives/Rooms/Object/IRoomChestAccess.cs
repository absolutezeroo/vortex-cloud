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
}
