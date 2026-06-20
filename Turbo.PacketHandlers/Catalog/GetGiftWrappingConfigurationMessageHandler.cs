using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Snapshots.Catalog;

namespace Turbo.PacketHandlers.Catalog;

public class GetGiftWrappingConfigurationMessageHandler
    : IMessageHandler<GetGiftWrappingConfigurationMessage>
{
    public async ValueTask HandleAsync(
        GetGiftWrappingConfigurationMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(
                new GiftWrappingConfigurationEventMessageComposer
                {
                    Configuration = new GiftWrappingConfigurationSnapshot(
                        IsWrappingEnabled: true,
                        WrappingPrice: 0,
                        StuffTypes: [1, 2, 3, 4, 5, 6, 7],
                        BoxTypes: [1, 2, 3, 4, 5, 6, 7],
                        RibbonTypes: [1, 2, 3, 4, 5, 6, 7],
                        DefaultStuffTypes: []
                    ),
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
