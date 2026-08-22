using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Locking a chest, or having it lock itself again after use.
/// </summary>
/// <remarks>
/// <see cref="SetWiredChestLockMessage.RequestedCapacity" /> is not forwarded. Capacity is bought,
/// not asked for, and this message arrives straight from a text box on the client.
/// </remarks>
public class SetWiredChestLockMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetWiredChestLockMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SetWiredChestLockMessage message,
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
            .SetWiredChestLockAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                message.Locked,
                message.AutoLock,
                ct
            )
            .ConfigureAwait(false);
    }
}
