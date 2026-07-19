using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.PacketHandlers.Navigator;

/// <summary>ForwardData's known string values (see the incoming message doc comment) come from a
/// client-side enum this codebase doesn't have a copy of. The only serverside-handled case observed
/// is a generic "put me somewhere" link, so every ForwardData value falls back to a random currently
/// populated room -- no attempt is made to special-case individual ForwardData strings without a
/// verified client reference.</summary>
public class ForwardToSomeRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ForwardToSomeRoomMessage>
{
    public async ValueTask HandleAsync(
        ForwardToSomeRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<RoomSummarySnapshot> activeRooms = await grainFactory
            .GetRoomDirectoryGrain()
            .GetActiveRoomsAsync()
            .ConfigureAwait(false);

        if (activeRooms.IsEmpty)
        {
            return;
        }

        RoomSummarySnapshot target = activeRooms[Random.Shared.Next(activeRooms.Length)];

        await RoomForwardHelper
            .SendGuestRoomResultAsync(
                grainFactory,
                ctx,
                target.RoomId,
                enterRoom: true,
                roomForward: true,
                ct
            )
            .ConfigureAwait(false);
    }
}
