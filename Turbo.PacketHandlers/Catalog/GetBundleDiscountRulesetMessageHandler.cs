using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Snapshots.Catalog;

namespace Turbo.PacketHandlers.Catalog;

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
