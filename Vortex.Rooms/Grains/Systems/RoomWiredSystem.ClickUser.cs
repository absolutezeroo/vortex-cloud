using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

namespace Vortex.Rooms.Grains.Systems;

public sealed partial class RoomWiredSystem
{
    /// <summary>
    /// What the room's click-user boxes mean for the client, read from the live trigger registry.
    /// </summary>
    /// <remarks>
    /// The registry is rebuilt here when dirty rather than waited on: the answer is needed on room
    /// entry and on a click, and neither is a tick. Rebuilding is the same work the next tick would
    /// have done anyway, and skipping it would report "no click-user box" for a room that has one
    /// simply because nobody had ticked yet — which the client caches for the whole visit.
    /// </remarks>
    public async Task<WiredClickUserSnapshot> GetClickUserStateAsync(CancellationToken ct)
    {
        if (_triggers.IsDirty)
        {
            await _triggers.RebuildAsync(ct);
        }

        IReadOnlyList<FurnitureWiredTriggerLogic> listening = _triggers.Listening(
            typeof(PlayerClickedPlayerEvent)
        );

        bool present = false;
        bool blocksMenu = false;

        foreach (FurnitureWiredTriggerLogic trigger in listening)
        {
            if (trigger is not WiredTriggerClickUser clickUser)
            {
                continue;
            }

            present = true;

            if (clickUser.BlocksMenuOpen)
            {
                blocksMenu = true;

                break;
            }
        }

        return new WiredClickUserSnapshot { Present = present, BlocksMenu = blocksMenu };
    }
}
