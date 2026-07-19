using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Lobby;

namespace Vortex.PacketHandlers.Game.Lobby;

public class class_165MessageHandler : IMessageHandler<class_165Message>
{
    public async ValueTask HandleAsync(
        class_165Message message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
