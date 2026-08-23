using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Landingview;

namespace Vortex.PacketHandlers.Landingview;

public class GetPromoArticlesMessageHandler : IMessageHandler<GetPromoArticlesMessage>
{
    public async ValueTask HandleAsync(
        GetPromoArticlesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
