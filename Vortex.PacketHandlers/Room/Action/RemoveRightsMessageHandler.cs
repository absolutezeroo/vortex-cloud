using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Action;

public class RemoveRightsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveRightsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveRightsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.TargetUserIds.IsEmpty)
        {
            return;
        }

        ImmutableArray<PlayerId> targets = [.. message.TargetUserIds.Select(id => (PlayerId)id)];

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);
        await roomGrain.RemoveRightsAsync(ctx.PlayerId, targets, ct).ConfigureAwait(false);
    }
}
