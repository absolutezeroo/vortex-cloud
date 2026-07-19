using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class GetCatalogPageWithEarliestExpiryMessageHandler(ILtdScheduleService ltdSchedule)
    : IMessageHandler<GetCatalogPageWithEarliestExpiryMessage>
{
    public async ValueTask HandleAsync(
        GetCatalogPageWithEarliestExpiryMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        CatalogPageExpirySnapshot expiry = await ltdSchedule
            .GetPageWithEarliestExpiryAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CatalogPageWithEarliestExpiryMessageComposer
                {
                    PageName = expiry.PageName,
                    SecondsToExpiry = expiry.SecondsToExpiry,
                    Image = expiry.Image,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
