using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Furni;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.PacketHandlers.Inventory.Furni;

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
