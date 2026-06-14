using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Competition;
using Turbo.Primitives.Messages.Outgoing.Competition;

namespace Turbo.PacketHandlers.Competition;

public class GetCurrentTimingCodeMessageHandler : IMessageHandler<GetCurrentTimingCodeMessage>
{
    public async ValueTask HandleAsync(
        GetCurrentTimingCodeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(
                new CurrentTimingCodeMessageComposer
                {
                    SlotConfig = message.SlotConfig,
                    Code = string.Empty,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
