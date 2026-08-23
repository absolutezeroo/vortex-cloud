using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Bots;
using Vortex.Protocol.Messages.Outgoing.Room.Bots;

namespace Vortex.PacketHandlers.Room.Bots;

public class GetBotCommandConfigurationDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetBotCommandConfigurationDataMessage>
{
    public async ValueTask HandleAsync(
        GetBotCommandConfigurationDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.BotId <= 0)
        {
            return;
        }

        string? data = await grainFactory
            .GetRoomBots(ctx.RoomId)
            .GetBotSkillAsync(message.BotId, message.CommandId, ct)
            .ConfigureAwait(false);

        // Null means the bot is not in this room at all; staying quiet leaves the dialog closed
        // rather than opening it onto a bot that is not there.
        if (data is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new BotCommandConfigurationMessageComposer
                {
                    BotId = message.BotId,
                    CommandId = message.CommandId,
                    Data = data,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
