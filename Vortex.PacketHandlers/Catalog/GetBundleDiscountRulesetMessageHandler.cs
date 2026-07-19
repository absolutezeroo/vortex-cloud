using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Snapshots.Catalog;

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
                        MaxPurchaseSize: 100,
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
