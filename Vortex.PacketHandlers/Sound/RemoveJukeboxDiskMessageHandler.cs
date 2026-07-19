using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Sound;

namespace Vortex.PacketHandlers.Sound;

public class RemoveJukeboxDiskMessageHandler : IMessageHandler<RemoveJukeboxDiskMessage>
{
    public async ValueTask HandleAsync(
        RemoveJukeboxDiskMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
