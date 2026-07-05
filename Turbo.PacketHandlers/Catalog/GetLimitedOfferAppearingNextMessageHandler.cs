using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;

namespace Turbo.PacketHandlers.Catalog;

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
