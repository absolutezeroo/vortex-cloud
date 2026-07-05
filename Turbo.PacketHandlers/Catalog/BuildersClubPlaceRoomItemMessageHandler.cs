using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Catalog;

/// <summary>
/// Free direct-to-room placement for Builders Club subscribers -- no currency debit, gated by an
/// active subscription and the subscription's furni-count limit (checked atomically inside
/// IInventoryGrain.GrantSingleFurnitureIfUnderLimitAsync). Reuses the exact same room-security-gated
/// IRoomGrain.PlaceFloorItemAsync the normal inventory-placement path uses, so ownership/rights
/// enforcement isn't duplicated here.
/// </summary>
public class BuildersClubPlaceRoomItemMessageHandler(
    IGrainFactory grainFactory,
    ICatalogService catalogService,
    IBuildersClubService buildersClubService
) : IMessageHandler<BuildersClubPlaceRoomItemMessage>
{
    public async ValueTask HandleAsync(
        BuildersClubPlaceRoomItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        FurnitureItemSnapshot? item = await BuildersClubPlacementHelper
            .TryGrantEligibleItemAsync(
                grainFactory,
                catalogService,
                buildersClubService,
                ctx.PlayerId,
                message.OfferId,
                message.ExtraParam,
                ProductType.Floor,
                ct
            )
            .ConfigureAwait(false);

        if (item is null)
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);

        bool placed = await roomGrain
            .PlaceFloorItemAsync(
                ctx.AsActionContext(),
                item,
                message.X,
                message.Y,
                (Rotation)message.Direction,
                ct
            )
            .ConfigureAwait(false);

        if (!placed)
        {
            await grainFactory
                .GetInventoryGrain(ctx.PlayerId)
                .RemoveFurnitureAsync(item.ItemId, ct)
                .ConfigureAwait(false);
        }
    }
}
