using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.PacketHandlers.Navigator;

public class ForwardToARandomPromotedRoomMessageHandler(
    IGrainFactory grainFactory,
    INavigatorProvider navigatorProvider
) : IMessageHandler<ForwardToARandomPromotedRoomMessage>
{
    public async ValueTask HandleAsync(
        ForwardToARandomPromotedRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        List<RoomInfoSnapshot> promoted = await navigatorProvider
            .GetPromotedRoomsAsync(message.Category, ct)
            .ConfigureAwait(false);

        if (promoted.Count == 0)
        {
            return;
        }

        RoomInfoSnapshot target = promoted[Random.Shared.Next(promoted.Count)];

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
