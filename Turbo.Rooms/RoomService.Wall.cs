using System;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Inventory.Grains;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Grains;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Snapshots.Furniture;

namespace Turbo.Rooms;

internal sealed partial class RoomService
{
    public async Task PlaceWallItemInRoomAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (ctx.Origin != ActionOrigin.Player || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        try
        {
            IInventoryGrain inventoryGrain = _grainFactory.GetInventoryGrain(ctx.PlayerId);

            FurnitureItemSnapshot? snapshot = await inventoryGrain
                .GetItemSnapshotAsync(itemId, ct)
                .ConfigureAwait(false);

            if (snapshot is null || snapshot.Definition.ProductType != ProductType.Wall)
            {
                return;
            }

            IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

            if (
                !await roomGrain
                    .PlaceWallItemAsync(ctx, snapshot, x, y, z, wallOffset, rot, ct)
                    .ConfigureAwait(false)
            )
            {
                // failed
                return;
            }
        }
        catch (Exception) { }
    }

    public async Task MoveWallItemInRoomAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Altitude z,
        int wallOffset,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || itemId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        if (
            await roomGrain
                .MoveWallItemByIdAsync(ctx, itemId, x, y, z, wallOffset, rot, ct)
                .ConfigureAwait(false)
        )
        {
            return;
        }

        RoomWallItemSnapshot? item = await roomGrain
            .GetWallItemSnapshotByIdAsync(itemId, ct)
            .ConfigureAwait(false);

        if (item is null)
        {
            return;
        }

        ISessionContext? session = _sessionGateway.GetSession(ctx.SessionKey);

        if (session is not null)
        {
            await session
                .SendComposerAsync(new ItemUpdateMessageComposer { WallItem = item }, ct)
                .ConfigureAwait(false);
        }
    }
}
