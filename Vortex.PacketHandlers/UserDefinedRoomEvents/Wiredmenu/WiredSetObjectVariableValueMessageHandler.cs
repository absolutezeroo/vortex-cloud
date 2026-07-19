using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredSetObjectVariableValueMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredSetObjectVariableValueMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredSetObjectVariableValueMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        bool success = await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .SetPermanentVariableAsync(
                (WiredVariableTargetType)message.EntityType,
                message.EntityId,
                message.VariableId,
                message.Value,
                message.Action,
                ct
            )
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(
                new WiredSetUserPermanentVariableResultComposer() { Success = success },
                ct
            )
            .ConfigureAwait(false);
    }
}
