using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Fishing;

namespace Vortex.PacketHandlers.Fishing;

/// <summary>
/// Starts a fishing session at the spot the player clicked. Vortex-specific: no AS3 or Habbo
/// equivalent — see the client's <c>docs/vortex-original/fishing.md</c>.
/// </summary>
/// <remarks>
/// Forwards and nothing else. Whether the furniture really is a spot, whether the player is high
/// enough level and whether they may fish at all are the session grain's to decide, because they are
/// the checks a client must not be able to talk its way past — and resolving them here would put
/// them one call away from the state they guard.
/// </remarks>
public class VortexStartFishingMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<VortexStartFishingMessage>
{
    public async ValueTask HandleAsync(
        VortexStartFishingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // A session belongs to a player in a room. Without either there is nothing to start, and
        // activating a grain on the unbound session's -1 would throw out of the pipeline.
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        await grainFactory
            .GetFishingSessionGrain(ctx.PlayerId)
            .StartAsync(ctx.RoomId, message.SpotObjectId, ct)
            .ConfigureAwait(false);
    }
}
