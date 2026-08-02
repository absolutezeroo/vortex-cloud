using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Avatar;

public class SignMessageHandler(IGrainFactory grainFactory) : IMessageHandler<SignMessage>
{
    /// <summary>Highest sign the client has artwork for (0-10 numbers, 11-17 the specials).</summary>
    private const int MaxSignId = 17;

    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SignMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // 0-17 is the full sign set the client can render; anything else would leave the avatar
        // stuck showing a status the client cannot draw.
        if (
            ctx is null
            || ctx.PlayerId <= 0
            || ctx.RoomId <= 0
            || message.SignId < 0
            || message.SignId > MaxSignId
        )
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);
        await roomGrain
            .SetAvatarSignAsync(ctx.AsActionContext(), message.SignId, ct)
            .ConfigureAwait(false);
    }
}
