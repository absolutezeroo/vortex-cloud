using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Turbo.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

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
            .GetRoomGrain(ctx.RoomId)
            .GetPermanentVariablesForEntityAsync(
                (WiredVariableTargetType)message.EntityType,
                message.EntityId,
                ct
            )
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(
                new WiredUserPermanentVariablesComposer() { Snapshot = snapshot },
                ct
            )
            .ConfigureAwait(false);
    }
}
