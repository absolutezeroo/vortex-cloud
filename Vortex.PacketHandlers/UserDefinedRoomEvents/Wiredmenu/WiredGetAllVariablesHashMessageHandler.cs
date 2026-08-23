using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

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
            .GetRoomFurni(ctx.RoomId)
            .GetWiredVariablesSnapshotAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new WiredAllVariablesHashEventMessageComposer()
                {
                    AllVariablesHash = variables.AllVariablesHash,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
