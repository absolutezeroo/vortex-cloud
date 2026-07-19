using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetAllVariableHoldersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetAllVariableHoldersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetAllVariableHoldersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        (WiredVariableSnapshot Variable, List<(int ObjectId, int Value)> Holders)? result =
            await _grainFactory
                .GetRoomGrain(ctx.RoomId)
                .GetVariableHoldersByNameAsync(message.VariableId, ct)
                .ConfigureAwait(false);

        if (result is null)
        {
            return;
        }

        List<(RoomObjectId objectId, int value)> objectValues = result.Value.Holders.ConvertAll(h =>
            (new RoomObjectId(h.ObjectId), h.Value)
        );

        _ = ctx.SendComposerAsync(
                new WiredAllVariableHoldersEventMessageComposer
                {
                    VariableSnapshot = result.Value.Variable,
                    ObjectValues = objectValues,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
