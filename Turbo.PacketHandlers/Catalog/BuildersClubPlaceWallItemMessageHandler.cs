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

/// <summary>See BuildersClubPlaceRoomItemMessageHandler -- same free, subscription-gated,
/// limit-checked direct-to-room placement, for wall items.</summary>
public class BuildersClubPlaceWallItemMessageHandler(
    IGrainFactory grainFactory,
    ICatalogService catalogService,
    IBuildersClubService buildersClubService
) : IMessageHandler<BuildersClubPlaceWallItemMessage>
{
    public async ValueTask HandleAsync(
        BuildersClubPlaceWallItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || ctx.RoomId <= 0
            || string.IsNullOrEmpty(message.Location)
            || !WallLocationParser.TryParse(
                message.Location,
                out int x,
                out int y,
                out double z,
                out int wallOffset,
                out Rotation rot
            )
        )
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
                ProductType.Wall,
                ct
            )
            .ConfigureAwait(false);

        if (item is null)
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);

        bool placed = await roomGrain
            .PlaceWallItemAsync(ctx.AsActionContext(), item, x, y, z, wallOffset, rot, ct)
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
