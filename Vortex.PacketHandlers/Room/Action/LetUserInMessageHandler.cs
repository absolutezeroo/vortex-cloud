using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Room.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Room.Action;

public class LetUserInMessageHandler(IGrainFactory grainFactory, IRoomService roomService)
    : IMessageHandler<LetUserInMessage>
{
    public async ValueTask HandleAsync(
        LetUserInMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || string.IsNullOrEmpty(message.Username))
        {
            return;
        }

        PlayerId? targetPlayerId = await grainFactory
            .GetPlayerDirectoryGrain()
            .GetPlayerIdAsync(message.Username, ct)
            .ConfigureAwait(false);

        if (targetPlayerId is null)
        {
            return;
        }

        ActionContext actorCtx = ctx.AsActionContext();

        await roomService
            .AnswerDoorbellAsync(actorCtx, targetPlayerId.Value, message.CanEnter, ct)
            .ConfigureAwait(false);
    }
}
