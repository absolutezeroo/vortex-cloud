using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;

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

        List<WiredErrorLogEntry> entries = await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .GetWiredErrorLogsAsync(ct)
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(
                new WiredErrorLogsEventMessageComposer() { Entries = entries },
                ct
            )
            .ConfigureAwait(false);
    }
}
