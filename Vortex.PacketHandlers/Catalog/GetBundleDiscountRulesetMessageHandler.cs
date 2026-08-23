using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Snapshots.Catalog;
using Vortex.Protocol.Messages.Incoming.Catalog;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class GetBundleDiscountRulesetMessageHandler
    : IMessageHandler<GetBundleDiscountRulesetMessage>
{
    public async ValueTask HandleAsync(
        GetBundleDiscountRulesetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(
                new BundleDiscountRulesetMessageComposer
                {
                    BundleDiscountRuleset = new BundleDiscountRulesetSnapshot(
                        MaxPurchaseSize: BundleDiscountRulesetSnapshot.DEFAULT_MAX_PURCHASE_SIZE,
                        BundleSize: 6,
                        BundleDiscountSize: 1,
                        BonusThreshold: 0,
                        AdditionalBonusDiscountThresholdQuantities: []
                    ),
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
