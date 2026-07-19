using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Directory;

namespace Vortex.PacketHandlers.Game.Directory;

public class Game2StartSnowWarMessageHandler : IMessageHandler<Game2StartSnowWarMessage>
{
    public async ValueTask HandleAsync(
        Game2StartSnowWarMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
