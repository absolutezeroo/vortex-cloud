using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Bots;

namespace Vortex.PacketHandlers.Room.Bots;

public class CommandBotMessageHandler : IMessageHandler<CommandBotMessage>
{
    public async ValueTask HandleAsync(
        CommandBotMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
