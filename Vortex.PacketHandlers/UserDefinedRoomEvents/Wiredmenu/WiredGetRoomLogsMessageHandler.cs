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

        WiredRoomLogPageSnapshot page = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetWiredRoomLogsPageAsync(
                message.Page,
                message.PageSize,
                message.LogLevelFilter,
                message.LogSourceFilter,
                message.Query,
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new WiredRoomLogsComposer
                {
                    TotalEntries = page.TotalEntries,
                    CurrentPage = page.CurrentPage,
                    Amount = page.Amount,
                    LogLevelFilter = page.LogLevelFilter,
                    LogSourceFilter = page.LogSourceFilter,
                    Query = page.Query,
                    Entries =
                    [
                        .. page.Entries.Select(e => new WiredRoomLogEntry
                        {
                            Id = e.Id,
                            LogLevel = e.LogLevel,
                            LogSource = e.LogSource,
                            Message = e.Message,
                            Timestamp = e.Timestamp,
                            TimestampStr = e.TimestampStr,
                        }),
                    ],
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
