using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Gifts;

namespace Vortex.PacketHandlers.Gifts;

public class VerifyCodeMessageHandler : IMessageHandler<VerifyCodeMessage>
{
    public async ValueTask HandleAsync(
        VerifyCodeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
