using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetRoomLogsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetRoomLogsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetRoomLogsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredRoomLogsComposer page = await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .GetWiredRoomLogsPageAsync(
                message.Page,
                message.PageSize,
                message.LogLevelFilter,
                message.LogSourceFilter,
                message.Query,
                ct
            )
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(page, ct).ConfigureAwait(false);
    }
}
