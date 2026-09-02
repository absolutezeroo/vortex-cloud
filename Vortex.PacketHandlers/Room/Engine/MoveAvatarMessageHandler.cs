using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class MoveAvatarMessageHandler(IRoomService roomService, IGrainFactory grainFactory)
    : IMessageHandler<MoveAvatarMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        MoveAvatarMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // Walking away puts the rod down, and it has to happen here rather than on the session's own
        // next step: the step is on a rolled delay of seconds, so the player would cross the room
        // still holding the rod with a line running back to the water. The grain returns
        // immediately when no session is running, which is the usual case for this packet, so this
        // costs one round trip on a message that already makes several.
        //
        // Not the only guard. Every other way an avatar moves — a wired teleport, a push, a roller —
        // sends no packet at all, and `FishingSessionGrain.SightAsync` re-checks reach for those.
        if (ctx.PlayerId > 0)
        {
            await grainFactory
                .GetFishingSessionGrain(ctx.PlayerId)
                .StopAsync(ct)
                .ConfigureAwait(false);
        }

        await _roomService
            .ClickTileAsync(
                ctx.AsActionContext(),
                message.TargetX,
                message.TargetY,
                message.TargetZKey,
                ct
            )
            .ConfigureAwait(false);
    }
}
