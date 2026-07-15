using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

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

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);

        await room.ClearWiredErrorLogsAsync(ct).ConfigureAwait(false);

        List<WiredErrorLogEntry> entries = await room.GetWiredErrorLogsAsync(ct)
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(
                new WiredErrorLogsEventMessageComposer() { Entries = entries },
                ct
            )
            .ConfigureAwait(false);
    }
}
