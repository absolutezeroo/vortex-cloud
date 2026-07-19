using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Sound;

namespace Vortex.PacketHandlers.Sound;

public class GetJukeboxPlayListMessageHandler : IMessageHandler<GetJukeboxPlayListMessage>
{
    public async ValueTask HandleAsync(
        GetJukeboxPlayListMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
