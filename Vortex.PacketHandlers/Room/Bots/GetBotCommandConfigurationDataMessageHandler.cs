using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Bots;

namespace Vortex.PacketHandlers.Room.Bots;

public class GetBotCommandConfigurationDataMessageHandler
    : IMessageHandler<GetBotCommandConfigurationDataMessage>
{
    public async ValueTask HandleAsync(
        GetBotCommandConfigurationDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
