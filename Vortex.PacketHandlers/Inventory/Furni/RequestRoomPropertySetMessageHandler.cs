using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Furni;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.PacketHandlers.Inventory.Furni;

public class RequestRoomPropertySetMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RequestRoomPropertySetMessage>
{
    public async ValueTask HandleAsync(
        RequestRoomPropertySetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        RoomSnapshot snapshot = await grainFactory
            .GetRoomGrain(message.RoomId)
            .GetSnapshotAsync()
            .ConfigureAwait(false);

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);

        await presence
            .SendComposerAsync(
                new RoomPropertyMessageComposer
                {
                    Key = RoomPropertyType.WALLPAPER,
                    Value = snapshot.PaintWall.ToString(CultureInfo.InvariantCulture),
                }
            )
            .ConfigureAwait(false);
        await presence
            .SendComposerAsync(
                new RoomPropertyMessageComposer
                {
                    Key = RoomPropertyType.FLOOR,
                    Value = snapshot.PaintFloor.ToString(CultureInfo.InvariantCulture),
                }
            )
            .ConfigureAwait(false);
        await presence
            .SendComposerAsync(
                new RoomPropertyMessageComposer
                {
                    Key = RoomPropertyType.LANDSCAPE,
                    Value = snapshot.PaintLandscape.ToString(CultureInfo.InvariantCulture),
                }
            )
            .ConfigureAwait(false);
        await presence
            .SendComposerAsync(
                // No animated-landscape state exists on RoomEntity yet -- always "not animated"
                // until that concept is modeled, rather than fabricating a value.
                new RoomPropertyMessageComposer
                {
                    Key = RoomPropertyType.LANDSCAPEANIM,
                    Value = "0",
                }
            )
            .ConfigureAwait(false);
    }
}
