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

/// <summary>Fires when a contract transaction went through. The player the transaction was waiting on is the triggering user, so
/// anything downstream that acts on "the triggerer" acts on them.</summary>
[RoomObjectLogic("wf_trg_transaction_complete")]
public class WiredTriggerTransactionCompleted(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.TRANSACTION_COMPLETED;

    public override List<Type> SupportedEventTypes { get; } =
    [typeof(WiredTransactionCompletedEvent)];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (ctx.Event is not WiredTransactionCompletedEvent evt)
        {
            return Task.FromResult(false);
        }

        ctx.Selected.SelectedPlayerIds.Add(evt.PlayerId);

        return Task.FromResult(true);
    }
}
