using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;

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
        await ctx.SendComposerAsync(new GiftWrappingConfigurationEventMessageComposer(), ct)
            .ConfigureAwait(false);
    }
}
