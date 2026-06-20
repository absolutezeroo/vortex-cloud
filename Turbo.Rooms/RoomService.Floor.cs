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
    public async Task PlaceFloorItemInRoomAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (
            ctx.Origin != ActionOrigin.Player
            || ctx.PlayerId <= 0
            || ctx.RoomId <= 0
            || itemId <= 0
        )
        {
            return;
        }

        try
        {
            IInventoryGrain inventoryGrain = _grainFactory.GetInventoryGrain(ctx.PlayerId);

            FurnitureItemSnapshot? snapshot = await inventoryGrain
                .GetItemSnapshotAsync(itemId, ct)
                .ConfigureAwait(false);

            if (snapshot is null || snapshot.Definition.ProductType != ProductType.Floor)
            {
                return;
            }

            IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

            if (
                !await roomGrain
                    .PlaceFloorItemAsync(ctx, snapshot, x, y, rot, ct)
                    .ConfigureAwait(false)
            )
            {
                // failed
                return;
            }
        }
        catch (Exception) { }
    }

    public async Task MoveFloorItemInRoomAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
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
            await roomGrain.MoveFloorItemByIdAsync(ctx, itemId, x, y, rot, ct).ConfigureAwait(false)
        )
        {
            return;
        }

        RoomFloorItemSnapshot? item = await roomGrain
            .GetFloorItemSnapshotByIdAsync(itemId, ct)
            .ConfigureAwait(false);

        if (item is null)
        {
            return;
        }

        ISessionContext? session = _sessionGateway.GetSession(ctx.SessionKey);

        if (session is not null)
        {
            await session
                .SendComposerAsync(new ObjectUpdateMessageComposer { FloorItem = item }, ct)
                .ConfigureAwait(false);
        }
    }
}
