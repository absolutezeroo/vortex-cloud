using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.GroupForums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

/// <summary>
/// Persists how far the player has read into one or more guild forums. The client batches every
/// forum it wants to mark into a single packet, both when leaving a forum view and when using
/// "mark all forums as read".
/// </summary>
public class UpdateForumReadMarkerMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpdateForumReadMarkerMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UpdateForumReadMarkerMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.Markers.Length == 0)
        {
            return;
        }

        await _grainFactory
            .GetGroupDirectoryGrain()
            .UpdateForumReadMarkersAsync(ctx.PlayerId, message.Markers, ct)
            .ConfigureAwait(false);
    }
}
