using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Sound;

namespace Vortex.PacketHandlers.Sound;

public class GetNowPlayingMessageHandler : IMessageHandler<GetNowPlayingMessage>
{
    public async ValueTask HandleAsync(
        GetNowPlayingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
