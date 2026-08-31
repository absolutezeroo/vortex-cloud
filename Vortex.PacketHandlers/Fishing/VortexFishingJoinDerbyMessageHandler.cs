using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Fishing;

namespace Vortex.PacketHandlers.Fishing;

/// <summary>
/// Enters the player in the running fishing derby. Vortex-specific: no AS3 or Habbo equivalent, and
/// Vortex's own addition rather than a reconstruction — Origins has the Fishing Frenzy, not a
/// leaderboard.
/// </summary>
/// <remarks>
/// The grain answers a bool and pushes its own refusal, so nothing is done with the result here: the
/// player has already been told either the standings or <c>DerbyClosed</c>.
/// </remarks>
public class VortexFishingJoinDerbyMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<VortexFishingJoinDerbyMessage>
{
    public async ValueTask HandleAsync(
        VortexFishingJoinDerbyMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetFishingDerbyGrain()
            .JoinAsync(ctx.PlayerId, message.DerbyId, ct)
            .ConfigureAwait(false);
    }
}
