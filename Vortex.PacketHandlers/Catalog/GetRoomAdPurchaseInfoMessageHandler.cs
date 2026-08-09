using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Catalog;

public class GetRoomAdPurchaseInfoMessageHandler(
    IGrainFactory grainFactory,
    INavigatorProvider navigatorProvider,
    IRoomAdvertisementService roomAdvertisements
) : IMessageHandler<GetRoomAdPurchaseInfoMessage>
{
    public async ValueTask HandleAsync(
        GetRoomAdPurchaseInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ClubSubscriptionSnapshot sub = await grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetClubSubscriptionAsync(ct)
            .ConfigureAwait(false);

        // A player cannot own more rooms than this, so it doubles as the cap on the dialog's list.
        int maxRooms = await grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(RoomsConfig.MaxRoomsPerPlayerKey, RoomsConfig.MaxRoomsPerPlayerDefault)
            .ConfigureAwait(false);

        List<RoomInfoSnapshot> ownedRooms = await navigatorProvider
            .GetRoomsByOwnerAsync(ctx.PlayerId, maxRooms, ct)
            .ConfigureAwait(false);

        RoomAdRoomEntry[] roomEntries = await Task.WhenAll(
                ownedRooms.Select(async r => new RoomAdRoomEntry
                {
                    RoomId = r.RoomId,
                    RoomName = r.Name,
                    IsEventRoom = await roomAdvertisements
                        .HasActiveAdvertisementAsync(r.RoomId, ct)
                        .ConfigureAwait(false),
                })
            )
            .ConfigureAwait(false);

        ImmutableArray<RoomAdRoomEntry> rooms = [.. roomEntries];

        await ctx.SendComposerAsync(
                new RoomAdPurchaseInfoEventMessageComposer { IsVip = sub.IsVip, Rooms = rooms },
                ct
            )
            .ConfigureAwait(false);
    }
}
