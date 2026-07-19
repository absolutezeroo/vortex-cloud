using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Hotlooks;

namespace Vortex.PacketHandlers.Hotlooks;

public class GetHotLooksMessageHandler : IMessageHandler<GetHotLooksMessage>
{
    public async ValueTask HandleAsync(
        GetHotLooksMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
