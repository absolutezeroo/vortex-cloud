using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class GetLimitedOfferAppearingNextMessageHandler(ILtdScheduleService ltdSchedule)
    : IMessageHandler<GetLimitedOfferAppearingNextMessage>
{
    public async ValueTask HandleAsync(
        GetLimitedOfferAppearingNextMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        LimitedOfferAppearanceSnapshot appearance = await ltdSchedule
            .GetNextAppearanceAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new LimitedOfferAppearingNextMessageComposer
                {
                    AppearsInSeconds = appearance.AppearsInSeconds,
                    PageId = appearance.PageId,
                    OfferId = appearance.OfferId,
                    ProductType = appearance.ProductType,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
