using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Avatar;

namespace Vortex.PacketHandlers.Avatar;

public class GetWardrobeMessageHandler : IMessageHandler<GetWardrobeMessage>
{
    public async ValueTask HandleAsync(
        GetWardrobeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
