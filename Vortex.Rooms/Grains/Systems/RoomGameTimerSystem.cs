using System.Threading;
using System.Threading.Tasks;
using Vortex.Rooms.Object.Logic.Furniture.Floor;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Ticks every game-timer furni (<see cref="FurnitureGameTimerLogic"/>) in the room once per frame so
/// running countdowns advance and their displayed value updates. It is the non-wired counterpart to
/// the wired counter tick — game timers are ordinary furni, so they aren't part of any wired stack.
/// Stopped timers short-circuit in <see cref="FurnitureGameTimerLogic.AdvanceAsync"/>, and the item
/// index makes the lookup O(timers in the room) instead of a full-room scan per 50 ms frame.
/// </summary>
public sealed class RoomGameTimerSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    public async Task ProcessAsync(long now, CancellationToken ct)
    {
        foreach (
            FurnitureGameTimerLogic timer in _roomGrain._state.ItemIndex.LogicsOf<FurnitureGameTimerLogic>()
        )
        {
            await timer.AdvanceAsync(now, ct);
        }
    }
}
