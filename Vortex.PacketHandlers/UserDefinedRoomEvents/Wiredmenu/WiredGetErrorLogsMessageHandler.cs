using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetErrorLogsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetErrorLogsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetErrorLogsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        List<WiredErrorLogSnapshot> entries = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetWiredErrorLogsAsync(ct)
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
