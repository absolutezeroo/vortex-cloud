using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;

namespace Vortex.PacketHandlers.Navigator;

/// <summary>Client-side analytics hook for the room-ads "Events" tab (impression tracking for the
/// room-ad purchase upsell) -- see RoomAdEventTabAdClickedMessageHandler.</summary>
public class RoomAdEventTabViewedMessageHandler(ILogger<RoomAdEventTabViewedMessageHandler> logger)
    : IMessageHandler<RoomAdEventTabViewedMessage>
{
    public async ValueTask HandleAsync(
        RoomAdEventTabViewedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        logger.LogInformation("RoomAd tab viewed: Player={PlayerId}", ctx.PlayerId);

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
