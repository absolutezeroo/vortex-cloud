using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.Inventory.Furni;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

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
            .GetRoomCore(message.RoomId)
            .GetSnapshotAsync()
            .ConfigureAwait(false);

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);

        await presence
            .SendComposerAsync(
                new RoomPropertyMessageComposer
                {
                    Key = RoomPropertyType.WALLPAPER,
                    Value = DecorationOrDefault(snapshot.PaintWall),
                }
            )
            .ConfigureAwait(false);
        await presence
            .SendComposerAsync(
                new RoomPropertyMessageComposer
                {
                    Key = RoomPropertyType.FLOOR,
                    Value = DecorationOrDefault(snapshot.PaintFloor),
                }
            )
            .ConfigureAwait(false);
        await presence
            .SendComposerAsync(
                new RoomPropertyMessageComposer
                {
                    Key = RoomPropertyType.LANDSCAPE,
                    Value = DecorationOrDefault(snapshot.PaintLandscape),
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

    /// <summary>A room that has never been decorated stores no id; "0" is the client's default
    /// surface, and an empty string would render as a missing asset.</summary>
    private static string DecorationOrDefault(string value) =>
        string.IsNullOrWhiteSpace(value) ? "0" : value;
}
