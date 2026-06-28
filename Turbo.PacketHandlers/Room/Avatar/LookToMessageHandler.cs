using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Avatar;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Room.Avatar;

public class LookToMessageHandler(IGrainFactory grainFactory) : IMessageHandler<LookToMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        LookToMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);
        await roomGrain
            .LookToAvatarAsync(ctx.AsActionContext(), message.X, message.Y, ct)
            .ConfigureAwait(false);
    }
}
