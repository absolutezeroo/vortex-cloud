using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class GetSeasonalCalendarDailyOfferMessageHandler
    : IMessageHandler<GetSeasonalCalendarDailyOfferMessage>
{
    public async ValueTask HandleAsync(
        GetSeasonalCalendarDailyOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
