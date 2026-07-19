using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

/// <summary>
/// "Bonus rare" is a promotional campaign ("buy N total credits this month, get item X free") --
/// no such campaign concept/entity exists in this codebase. ProductClassId = -1 is the client's own
/// "no active promotion, hide the widget" sentinel (BonusRarePromoWidget: `visible = classId != -1`),
/// so this is a truthful "nothing configured" reply, not a stub standing in for a real campaign.
/// </summary>
public class GetBonusRareInfoMessageHandler : IMessageHandler<GetBonusRareInfoMessage>
{
    public async ValueTask HandleAsync(
        GetBonusRareInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(
                new BonusRareInfoMessageComposer
                {
                    ProductType = string.Empty,
                    ProductClassId = -1,
                    TotalCoinsForBonus = 0,
                    CoinsStillRequiredToBuy = 0,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
