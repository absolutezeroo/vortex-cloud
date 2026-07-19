using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetAllVariablesHashMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetAllVariablesHashMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetAllVariablesHashMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredVariablesSnapshot variables = await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .GetWiredVariablesSnapshotAsync(ct)
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(
                new WiredAllVariablesHashEventMessageComposer()
                {
                    AllVariablesHash = variables.AllVariablesHash,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
