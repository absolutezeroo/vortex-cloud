using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Locking or unlocking every chest in the room.
/// </summary>
public class SetAllWiredChestLocksMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetAllWiredChestLocksMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SetAllWiredChestLocksMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .SetAllWiredChestLocksAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.Locked,
                ct
            )
            .ConfigureAwait(false);
    }
}
