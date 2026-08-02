using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// Placement validation and the wired engine's room-level knobs, as furniture logic reaches them.
/// Not a grain contract. Only <see cref="ValidateFloorItemPlacementAsync"/> is asynchronous, because
/// it is the only one that is asynchronous underneath.
/// </summary>
public interface IRoomFurniAccess
{
    /// <summary>Whether this floor item may sit at this position and rotation.</summary>
    Task<bool> ValidateFloorItemPlacementAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot
    );

    /// <summary>The live wired variable behind an id, or null when it no longer exists.</summary>
    IWiredVariable? GetVariableById(WiredVariableId id);

    /// <summary>Queues the brief visual flash a wired box shows when it fires.</summary>
    void ScheduleFlashRevert(RoomObjectId objectId);

    /// <summary>Re-anchors every wired timer in the room to the current room clock.</summary>
    void ResetTimers();
}
