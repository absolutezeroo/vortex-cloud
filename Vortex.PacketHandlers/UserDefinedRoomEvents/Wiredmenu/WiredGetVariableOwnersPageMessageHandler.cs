using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetVariableOwnersPageMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetVariableOwnersPageMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetVariableOwnersPageMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredVariableOwnersPageSnapshot page = await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .GetVariableOwnersPageAsync(
                message.VariableId,
                message.Page,
                message.PageSize,
                message.UserTypeFilter,
                message.SortTypeFilter,
                ct
            )
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(new WiredUserVariablesListComposer() { Page = page }, ct)
            .ConfigureAwait(false);
    }
}
