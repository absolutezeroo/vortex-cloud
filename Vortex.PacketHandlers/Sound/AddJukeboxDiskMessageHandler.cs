using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Sound;

namespace Vortex.PacketHandlers.Sound;

public class AddJukeboxDiskMessageHandler : IMessageHandler<AddJukeboxDiskMessage>
{
    public async ValueTask HandleAsync(
        AddJukeboxDiskMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
