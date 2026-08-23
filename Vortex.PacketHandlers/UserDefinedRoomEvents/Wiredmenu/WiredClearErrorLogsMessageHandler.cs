using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredClearErrorLogsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredClearErrorLogsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredClearErrorLogsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomWired room = _grainFactory.GetRoomWired(ctx.RoomId);

        await room.ClearWiredErrorLogsAsync(ct).ConfigureAwait(false);

        List<WiredErrorLogSnapshot> entries = await room.GetWiredErrorLogsAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new WiredErrorLogsEventMessageComposer
                {
                    Entries =
                    [
                        .. entries.Select(e => new WiredErrorLogEntry
                        {
                            ErrorId = e.ErrorId,
                            ErrorName = e.ErrorName,
                            Category = e.Category,
                            ThrowCount = e.ThrowCount,
                            MsSinceLastOccurrence = e.MsSinceLastOccurrence,
                        }),
                    ],
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
