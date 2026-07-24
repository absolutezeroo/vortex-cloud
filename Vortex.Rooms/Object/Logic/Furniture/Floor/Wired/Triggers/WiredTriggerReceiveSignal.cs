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

/// <summary>Fires when a send-signal action emits a signal (client ReceiveSignal.ts, no params). The
/// signal's furni and users are exposed to this stack as the SignalItems / SignalUsers sources — see
/// <see cref="SignalRoomEvent"/>. Was a stub bound to the wrong event that could never fire.</summary>
[RoomObjectLogic("wf_trg_recv_signal")]
public class WiredTriggerReceiveSignal(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.RECEIVE_SIGNAL;
    public override List<Type> SupportedEventTypes { get; } = [typeof(SignalRoomEvent)];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Event is SignalRoomEvent);
}
