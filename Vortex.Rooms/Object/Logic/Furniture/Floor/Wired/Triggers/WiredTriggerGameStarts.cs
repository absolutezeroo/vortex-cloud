using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

[RoomObjectLogic("wf_trg_game_starts")]
public class WiredTriggerGameStarts(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.GAME_STARTS;
    public override List<Type> SupportedEventTypes { get; } = [typeof(WiredGameStartedEvent)];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Event is WiredGameStartedEvent);
}
