using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

[RoomObjectLogic("wf_trg_user_exits_room")]
public class WiredTriggerHabboLeaveRoom(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.AVATAR_LEAVES_ROOM;
    public override List<Type> SupportedEventTypes { get; } = [typeof(PlayerLeftEvent)];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (ctx.Event is not PlayerLeftEvent evt)
        {
            return Task.FromResult(false);
        }

        // The leaving avatar is the triggering user for any downstream "triggered user" effects.
        ctx.Selected.SelectedPlayerIds.Add(evt.PlayerId);

        return Task.FromResult(true);
    }
}
