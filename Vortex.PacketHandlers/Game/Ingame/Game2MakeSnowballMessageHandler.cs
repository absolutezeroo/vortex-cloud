using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Ingame;

namespace Vortex.PacketHandlers.Game.Ingame;

public class Game2MakeSnowballMessageHandler : IMessageHandler<Game2MakeSnowballMessage>
{
    public async ValueTask HandleAsync(
        Game2MakeSnowballMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
