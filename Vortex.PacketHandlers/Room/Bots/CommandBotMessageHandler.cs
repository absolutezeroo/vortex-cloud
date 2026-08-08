using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Bots;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Bots;

public class CommandBotMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CommandBotMessage>
{
    public async ValueTask HandleAsync(
        CommandBotMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.BotId <= 0)
        {
            return;
        }

        await grainFactory
            .GetRoomBots(ctx.RoomId)
            .SetBotSkillAsync(
                ctx.AsActionContext(),
                message.BotId,
                message.CommandId,
                message.Data,
                ct
            )
            .ConfigureAwait(false);
    }
}
