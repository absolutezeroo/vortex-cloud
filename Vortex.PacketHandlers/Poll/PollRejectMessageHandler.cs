using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Poll;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Poll;

public class PollRejectMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PollRejectMessage>
{
    public async ValueTask HandleAsync(
        PollRejectMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerPollGrain(ctx.PlayerId)
            .RejectAsync(message.PollId, ct)
            .ConfigureAwait(false);
    }
}
