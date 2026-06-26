using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Room.Engine;

public class IssuePetCommandMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<IssuePetCommandMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        IssuePetCommandMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0 || message.CommandId < 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);

        await room.IssueCommandAsync(ctx.AsActionContext(), message.PetId, message.CommandId, ct)
            .ConfigureAwait(false);
    }
}
