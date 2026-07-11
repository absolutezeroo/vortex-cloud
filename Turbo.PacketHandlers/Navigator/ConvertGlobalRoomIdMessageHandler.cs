using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Logging;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Navigator;

/// <summary>FlatId is only ever observed as the decimal string form of a normal numeric RoomId (e.g.
/// deep-link URLs) -- there is no separate "global room id" concept elsewhere in this codebase, so
/// resolving it is just a parse-and-forward.</summary>
public class ConvertGlobalRoomIdMessageHandler(
    IGrainFactory grainFactory,
    ILogger<ConvertGlobalRoomIdMessageHandler> logger
) : IMessageHandler<ConvertGlobalRoomIdMessage>
{
    public async ValueTask HandleAsync(
        ConvertGlobalRoomIdMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || !int.TryParse(message.FlatId, out int roomId) || roomId <= 0)
        {
            return;
        }

        try
        {
            await RoomForwardHelper
                .SendGuestRoomResultAsync(
                    grainFactory,
                    ctx,
                    roomId,
                    enterRoom: true,
                    roomForward: true,
                    ct
                )
                .ConfigureAwait(false);
        }
        catch (TurboException ex)
        {
            logger.LogWarning(
                ex,
                "ConvertGlobalRoomId failed to resolve FlatId={FlatId} for Player={PlayerId}.",
                message.FlatId,
                ctx.PlayerId
            );
        }
    }
}
