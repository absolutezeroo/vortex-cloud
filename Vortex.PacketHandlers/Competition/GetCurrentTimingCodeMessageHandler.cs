using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Competition;
using Vortex.Protocol.Messages.Outgoing.Competition;

namespace Vortex.PacketHandlers.Competition;

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
