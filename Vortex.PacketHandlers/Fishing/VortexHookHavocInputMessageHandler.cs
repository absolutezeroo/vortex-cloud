using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Fishing;

namespace Vortex.PacketHandlers.Fishing;

/// <summary>
/// Hands a Hook Havoc attempt to the session that issued it. Vortex-specific: no AS3 or Habbo
/// equivalent.
/// </summary>
/// <remarks>
/// The timeline is input, never a verdict: the grain replays it against the seed it issued and
/// decides. The parser has already clamped its length, so nothing here can be asked to allocate on
/// a client's word.
/// </remarks>
public class VortexHookHavocInputMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<VortexHookHavocInputMessage>
{
    public async ValueTask HandleAsync(
        VortexHookHavocInputMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetFishingSessionGrain(ctx.PlayerId)
            .SubmitHookHavocAsync(message.Timeline, ct)
            .ConfigureAwait(false);
    }
}
