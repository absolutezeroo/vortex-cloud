using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

public abstract class FurnitureWiredTriggerLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredLogic(grainFactory, stuffDataFactory, ctx), IWiredTrigger
{
    public override WiredType WiredType => WiredType.Trigger;

    public virtual List<Type> SupportedEventTypes { get; } = [];

    public virtual Task<bool> MatchesEventAsync(RoomEvent evt, CancellationToken ct) =>
        Task.FromResult(SupportedEventTypes.Contains(evt.GetType()));

    public virtual Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(false);
}
