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
/// One row of the transaction log, opened.
/// </summary>
/// <remarks>
/// Sent by the overview table when a row is clicked. The window it opens listens for the answer and
/// shows whatever comes, so a row the server declines to explain simply leaves it empty.
/// </remarks>
public class GetWiredTransactionDetailsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetWiredTransactionDetailsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetWiredTransactionDetailsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredTransactionDetailsSnapshot? details = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetWiredTransactionDetailsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.TransactionId,
                ct
            )
            .ConfigureAwait(false);

        if (details is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new WiredTransactionDetailsMessageComposer { Details = details },
                ct
            )
            .ConfigureAwait(false);
    }
}
