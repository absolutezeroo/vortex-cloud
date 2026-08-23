using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Every chest in the room, for the wired menu's chests tab.
/// </summary>
public class GetWiredRoomTransactionsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetWiredRoomTransactionsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetWiredRoomTransactionsMessage message,
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
            .GetWiredRoomTransactionsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
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
