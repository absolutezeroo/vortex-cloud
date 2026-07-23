using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Rooms.Object.Logic.Furniture.Floor;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Ticks every game-timer furni (<see cref="FurnitureGameTimerLogic"/>) in the room once per frame so
/// running countdowns advance and their displayed value updates. It is the non-wired counterpart to
/// the wired counter tick — game timers are ordinary furni, so they aren't part of any wired stack.
/// Stopped timers short-circuit in <see cref="FurnitureGameTimerLogic.AdvanceAsync"/>, so this stays
/// cheap when nothing is running.
/// </summary>
public sealed class RoomGameTimerSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    public async Task ProcessAsync(long now, CancellationToken ct)
    {
        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (item.Logic is FurnitureGameTimerLogic timer)
            {
                await timer.AdvanceAsync(now, ct);
            }
        }
    }
}
