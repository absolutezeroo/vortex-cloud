using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// One chest's history.
/// </summary>
public class GetWiredChestTransactionsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetWiredChestTransactionsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetWiredChestTransactionsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredTransactionsSnapshot? page = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetWiredChestTransactionsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                message.PageSize,
                message.Page,
                ct
            )
            .ConfigureAwait(false);

        if (page is null)
        {
            return;
        }

        await ctx.SendComposerAsync(new WiredTransactionsMessageComposer { Page = page }, ct)
            .ConfigureAwait(false);
    }
}
