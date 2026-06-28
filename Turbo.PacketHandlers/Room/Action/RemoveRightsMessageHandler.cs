using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Room.Action;

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
