using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;

namespace Turbo.PacketHandlers.Navigator;

/// <summary>Client-side analytics hook for the room-ads "Events" tab (impression tracking for the
/// room-ad purchase upsell) -- no gameplay effect, logged for future analytics rather than a silent
/// no-op since it carries real data.</summary>
public class RoomAdEventTabAdClickedMessageHandler(
    ILogger<RoomAdEventTabAdClickedMessageHandler> logger
) : IMessageHandler<RoomAdEventTabAdClickedMessage>
{
    public async ValueTask HandleAsync(
        RoomAdEventTabAdClickedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "RoomAd tab click: Player={PlayerId} FlatId={FlatId} Name={RoomAdName} ExpiresInMin={ExpiresInMin}",
            ctx.PlayerId,
            message.FlatId,
            message.RoomAdName,
            message.RoomAdExpiresInMin
        );

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
