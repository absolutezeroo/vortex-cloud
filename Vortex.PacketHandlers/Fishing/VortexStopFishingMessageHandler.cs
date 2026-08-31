using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Fishing;

namespace Vortex.PacketHandlers.Fishing;

/// <summary>
/// Ends the player's fishing session. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// No room check, unlike the start: a player who has already walked out of the room is exactly the
/// one whose session most needs stopping.
/// </remarks>
public class VortexStopFishingMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<VortexStopFishingMessage>
{
    public async ValueTask HandleAsync(
        VortexStopFishingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory.GetFishingSessionGrain(ctx.PlayerId).StopAsync(ct).ConfigureAwait(false);
    }
}
