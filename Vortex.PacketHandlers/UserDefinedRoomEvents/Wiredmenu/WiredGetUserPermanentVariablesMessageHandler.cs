using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetUserPermanentVariablesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetUserPermanentVariablesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetUserPermanentVariablesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredPermanentVariablesSnapshot snapshot = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetPermanentVariablesForEntityAsync(
                (WiredVariableTargetType)message.EntityType,
                message.EntityId,
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new WiredUserPermanentVariablesComposer() { Snapshot = snapshot },
                ct
            )
            .ConfigureAwait(false);
    }
}
