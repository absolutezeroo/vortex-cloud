using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Action;
using Turbo.Primitives.Messages.Incoming.Room.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Room.Action;

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
