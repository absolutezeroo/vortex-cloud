using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// The guide asking where the person they are helping is, so their client can go there.
/// </summary>
/// <remarks>
/// Answered even when the requester is nowhere: the guide's client reads a zero as "not in a room"
/// and leaves the button alone, whereas silence leaves it looking broken.
/// </remarks>
public class GuideSessionGetRequesterRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionGetRequesterRoomMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionGetRequesterRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // The partner, not "the requester on the packet": the packet names nobody, and only the
        // session says whose room this guide is entitled to be told about.
        int requesterId = await grainFactory
            .GetGuideDirectoryGrain()
            .GetPartnerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (requesterId <= 0)
        {
            return;
        }

        RoomPointerSnapshot room = await grainFactory
            .GetPlayerPresenceGrain(requesterId)
            .GetActiveRoomAsync()
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new GuideSessionRequesterRoomMessageComposer
                {
                    RequesterRoomId = room.RoomId > 0 ? room.RoomId.Value : 0,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
