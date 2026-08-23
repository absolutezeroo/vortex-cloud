using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms.Snapshots;
using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// The guide asking the person they are helping to come to them.
/// </summary>
/// <remarks>
/// The room is read from where the guide actually is, not from anything the client sends: the
/// packet is empty, and taking a room id from a client would let a guide send someone anywhere.
/// Nothing is sent when the guide is not in a room — there would be nowhere to invite them to.
/// </remarks>
public class GuideSessionInviteRequesterMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionInviteRequesterMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionInviteRequesterMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int requesterId = await grainFactory
            .GetGuideDirectoryGrain()
            .GetPartnerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (requesterId <= 0)
        {
            return;
        }

        RoomPointerSnapshot room = await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .GetActiveRoomAsync()
            .ConfigureAwait(false);

        if (room.RoomId <= 0)
        {
            return;
        }

        RoomSummarySnapshot summary = await grainFactory
            .GetRoomCore(room.RoomId)
            .GetSummaryAsync()
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(requesterId)
            .SendComposerAsync(
                new GuideSessionInvitedToGuideRoomMessageComposer
                {
                    RoomId = room.RoomId.Value,
                    RoomName = summary.Name,
                }
            )
            .ConfigureAwait(false);
    }
}
