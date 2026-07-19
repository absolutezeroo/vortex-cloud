using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class OpenMysteryTrophyMessageHandler : IMessageHandler<OpenMysteryTrophyMessage>
{
    public async ValueTask HandleAsync(
        OpenMysteryTrophyMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
