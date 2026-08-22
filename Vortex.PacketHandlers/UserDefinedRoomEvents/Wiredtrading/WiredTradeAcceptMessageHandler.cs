using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Accepting a deposit — the button, then the confirmation dialog behind it.
/// </summary>
/// <remarks>
/// The client sends this twice for one completed trade and the room decides which one moves the
/// furniture; this only forwards, then tells the screen what happened. A completed deposit also
/// answers with the chest's own delta, so the chest window behind the trade shows the new rows
/// without asking for them.
/// </remarks>
public class WiredTradeAcceptMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredTradeAcceptMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredTradeAcceptMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredDepositSnapshot? deposit = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .AcceptWiredDepositAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.Confirm,
                ct
            )
            .ConfigureAwait(false);

        if (deposit is null)
        {
            return;
        }

        if (!deposit.Completed)
        {
            await ctx.SendComposerAsync(WiredTradeTable.For(ctx, deposit), ct)
                .ConfigureAwait(false);

            return;
        }

        await ctx.SendComposerAsync(new WiredTradeCompletedMessageComposer(), ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new WiredChestItemsUpdateMessageComposer
                {
                    ChestId = deposit.ChestId,
                    RemovedItemIds = [],
                    AddedItems = deposit.Items,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
