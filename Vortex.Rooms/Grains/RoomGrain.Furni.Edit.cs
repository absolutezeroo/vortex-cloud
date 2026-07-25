using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Object.Furniture.Wall;

namespace Vortex.Rooms.Grains;

/// <summary>
/// The in-client furni editor's write path. Vortex-specific: no AS3 or Habbo equivalent.
///
/// Deliberately does NOT route through RoomActionModule like the ordinary place/move path does.
/// That path exists to enforce what a player may do — room rights, rented-space containment,
/// stacking and tile-flag validation — and the editor's whole purpose is to bypass it: free
/// altitude, placement onto blocked tiles, ownership reassignment. What remains enforced here is
/// only what the room's own data structures require to stay coherent (map bounds, product-type
/// consistency); the authorization question is answered once, by the caller, against
/// <c>room.furni.edit</c>.
/// </summary>
public sealed partial class RoomGrain
{
    public async Task<string> ApplyFurniEditAsync(
        ActionContext ctx,
        VortexApplyFurniEditMessage edit,
        PlayerId? newOwnerId,
        string? newOwnerName,
        FurnitureDefinitionSnapshot? newDefinition,
        CancellationToken ct
    )
    {
        if (!_state.ItemsById.TryGetValue(edit.ObjectId, out IRoomItem? item))
        {
            return "item_not_found";
        }

        FurniEditField fields = edit.Fields;

        if (fields == FurniEditField.None)
        {
            return string.Empty;
        }

        // Taken before anything mutates, and while the item still has its original definition —
        // this is the "before" half of the audit record, and the only chance to capture it.
        RoomItemSnapshot before = item.GetSnapshot();

        try
        {
            if (fields.HasFlag(FurniEditField.Owner))
            {
                if (newOwnerId is null || newOwnerId.Value == PlayerId.Invalid)
                {
                    return "owner_not_found";
                }

                item.SetOwnerId(newOwnerId.Value);
                item.SetOwnerName(newOwnerName ?? string.Empty);

                // The room caches owner names by id for the infostand; a reassignment to a player
                // who owns nothing else here would otherwise leave the cache without an entry.
                _state.OwnerNamesById[newOwnerId.Value] = newOwnerName ?? string.Empty;
            }

            if (fields.HasFlag(FurniEditField.ExtraData))
            {
                item.SetExtraData(edit.ExtraData);
            }

            string transformError = ApplyFurniEditTransform(item, edit, fields);

            if (transformError.Length > 0)
            {
                return transformError;
            }

            // Last, because it replaces the item object outright: everything above has to already
            // be on the item so the rebuilt one inherits it.
            if (fields.HasFlag(FurniEditField.Definition))
            {
                (IRoomItem? swapped, string swapError) = await SwapFurniDefinitionAsync(
                    item,
                    newDefinition,
                    ct
                );

                if (swapError.Length > 0)
                {
                    return swapError;
                }

                item = swapped!;
            }
            else
            {
                await SendComposerToRoomAsync(item.GetUpdateComposer());

                if (fields.HasFlag(FurniEditField.ExtraData))
                {
                    await SendComposerToRoomAsync(item.GetRefreshStuffDataComposer());
                }
            }

            item.MarkDirty();

            await PublishFurniEditAuditAsync(ctx, before, item.GetSnapshot(), fields);

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Furni editor failed to apply {Fields} to item {ItemId} in room {RoomId}",
                fields,
                edit.ObjectId,
                _state.RoomId
            );

            return "edit_failed";
        }
    }

    /// <summary>
    /// Moves/rotates/raises the item per the mask. Unset flags keep the item's current value rather
    /// than the (undefined) wire value.
    /// </summary>
    private string ApplyFurniEditTransform(
        IRoomItem item,
        VortexApplyFurniEditMessage edit,
        FurniEditField fields
    )
    {
        bool wantsPosition = fields.HasFlag(FurniEditField.Position);
        bool wantsRotation = fields.HasFlag(FurniEditField.Rotation);
        bool wantsAltitude = fields.HasFlag(FurniEditField.Altitude);
        bool wantsWallOffset = fields.HasFlag(FurniEditField.WallOffset);

        if (!wantsPosition && !wantsRotation && !wantsAltitude && !wantsWallOffset)
        {
            return string.Empty;
        }

        int targetX = wantsPosition ? edit.X : item.X;
        int targetY = wantsPosition ? edit.Y : item.Y;
        Rotation targetRotation = wantsRotation ? edit.Rotation : item.Rotation;

        switch (item)
        {
            case IRoomFloorItem floor:
            {
                int tileIdx = MapModule.ToIdx(targetX, targetY);

                // The only placement rule the editor still honours. Not a policy check: the tile
                // arrays are indexed by this value, so an out-of-bounds move corrupts room state
                // rather than merely allowing something it shouldn't.
                if (!MapModule.InBounds(tileIdx))
                {
                    return "tile_out_of_bounds";
                }

                // Null altitude means "snap to the stack under the target tile", which is what the
                // ordinary move path does. An explicit value is honoured verbatim, including values
                // above MaxStackHeight — free altitude is the point of the editor.
                Altitude? targetZ = wantsAltitude ? Altitude.FromInt(edit.ZHundredths) : null;

                if (!MapModule.MoveFloorItem(floor, tileIdx, targetZ, targetRotation))
                {
                    return "move_failed";
                }

                return string.Empty;
            }

            case IRoomWallItem wall:
            {
                // Wall items are not on the tile grid: no bounds check applies, and MoveWallItem
                // takes every component non-optionally, so unset flags fall back to current values.
                Altitude targetZ = wantsAltitude ? Altitude.FromInt(edit.ZHundredths) : wall.Z;
                int targetWallOffset = wantsWallOffset ? edit.WallOffset : wall.WallOffset;

                if (
                    !MapModule.MoveWallItem(
                        wall,
                        targetX,
                        targetY,
                        targetZ,
                        targetRotation,
                        targetWallOffset
                    )
                )
                {
                    return "move_failed";
                }

                return string.Empty;
            }

            default:
                return "unsupported_item_type";
        }
    }

    /// <summary>
    /// Replaces the item's furniture definition. <c>RoomItem.Definition</c> is init-only, so this is
    /// a rebuild, not a mutation: the old object is detached from the room and a new one carrying
    /// the same object id and state is attached in its place.
    ///
    /// It deliberately does not call <c>ObjectModule.RemoveObjectAsync</c>, which would be the
    /// obvious way to detach. That method enqueues the item into the persistence grain with
    /// <c>remove: true</c>, and nothing clears that flag before the next flush — the row's
    /// <c>room_id</c> would be nulled a moment after we re-attached the replacement, and the item
    /// would silently vanish from the room the next time it loaded. It also runs
    /// <c>Logic.OnPickupAsync</c>, which is the pickup semantic and has side effects a swap must
    /// not trigger. The detach below is that method minus those two steps.
    /// </summary>
    private async Task<(IRoomItem? item, string error)> SwapFurniDefinitionAsync(
        IRoomItem item,
        FurnitureDefinitionSnapshot? definition,
        CancellationToken ct
    )
    {
        if (definition is null)
        {
            return (null, "definition_not_found");
        }

        // A floor item cannot become a wall item: RoomItemsProvider.CreateFromEntity picks the
        // concrete class from ProductType, so on the next room load the row would come back as a
        // different kind of object than the one the room is holding, placed by rules that never ran.
        if (definition.ProductType != item.Definition.ProductType)
        {
            return (null, "definition_product_type_mismatch");
        }

        if (definition.Id == item.Definition.Id)
        {
            return (item, string.Empty);
        }

        (int x, int y, Altitude z, Rotation rotation) = (item.X, item.Y, item.Z, item.Rotation);
        int wallOffset = item is IRoomWallItem oldWall ? oldWall.WallOffset : 0;
        string extraData = item.GetSnapshot().ExtraData;
        RoomObjectId objectId = item.ObjectId;
        PlayerId ownerId = item.OwnerId;
        string ownerName = item.OwnerName;

        // Removed with the OLD definition still in place, so the footprint cleared from the tile
        // stacks is the one that was actually occupied.
        if (!MapModule.RemoveItem(item))
        {
            return (null, "detach_failed");
        }

        await SendComposerToRoomAsync(item.GetRemoveComposer(-1));

        await item.Logic.OnDetachAsync(ct);

        item.SetAction(null);

        _state.ItemsById.Remove(objectId);

        IRoomItem? replacement = definition.ProductType switch
        {
            ProductType.Floor => new RoomFloorItem
            {
                ObjectId = objectId,
                OwnerId = ownerId,
                OwnerName = ownerName,
                Definition = definition,
            },
            ProductType.Wall => new RoomWallItem
            {
                ObjectId = objectId,
                OwnerId = ownerId,
                OwnerName = ownerName,
                Definition = definition,
            },
            _ => null,
        };

        if (replacement is null)
        {
            return (null, "definition_product_type_unsupported");
        }

        replacement.SetExtraData(extraData);

        // Position before attach, not after: AttatchObjectAsync adds the item to the tile stacks
        // using its current coordinates, so attaching first would register the new footprint at
        // (0,0) and leave a stale entry there once the real position was applied.
        replacement.SetPosition(x, y);
        replacement.SetPositionZ(z);
        replacement.SetRotation(rotation);

        if (replacement is IRoomWallItem newWall)
        {
            newWall.SetWallOffset(wallOffset);
        }

        if (!await ObjectModule.AttatchObjectAsync(replacement, ct))
        {
            return (null, "attach_failed");
        }

        await SendComposerToRoomAsync(replacement.GetAddComposer());

        string persistError = await PersistFurniDefinitionAsync(objectId, definition.Id, ct);

        if (persistError.Length > 0)
        {
            return (null, persistError);
        }

        return (replacement, string.Empty);
    }

    /// <summary>
    /// Writes the new <c>definition_id</c> straight to the row. The dirty-item flush cannot carry
    /// this: RoomPersistenceGrain writes owner, coordinates, rotation, extra data and wall offset,
    /// and never touches the definition column — so without this write the swap would be visible in
    /// the room until the next load and then silently revert.
    /// </summary>
    private async Task<string> PersistFurniDefinitionAsync(
        RoomObjectId objectId,
        int definitionId,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext db = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            int affected = await db
                .Furnitures.Where(f => f.Id == objectId.Value)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(f => f.FurnitureDefinitionEntityId, definitionId),
                    ct
                )
                .ConfigureAwait(true);

            return affected > 0 ? string.Empty : "definition_persist_failed";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Furni editor failed to persist definition {DefinitionId} for item {ItemId}",
                definitionId,
                objectId
            );

            return "definition_persist_failed";
        }
    }

    private async Task PublishFurniEditAuditAsync(
        ActionContext ctx,
        RoomItemSnapshot before,
        RoomItemSnapshot after,
        FurniEditField fields
    )
    {
        Dictionary<string, object?> changes = [];

        if (fields.HasFlag(FurniEditField.Position))
        {
            changes["position"] = new
            {
                before = new { x = before.X, y = before.Y },
                after = new { x = after.X, y = after.Y },
            };
        }

        if (fields.HasFlag(FurniEditField.Rotation))
        {
            changes["rotation"] = new
            {
                before = before.Rotation.ToString(),
                after = after.Rotation.ToString(),
            };
        }

        if (fields.HasFlag(FurniEditField.Altitude))
        {
            changes["altitude"] = new { before = before.Z.Value, after = after.Z.Value };
        }

        if (fields.HasFlag(FurniEditField.ExtraData))
        {
            changes["extraData"] = new { before = before.ExtraData, after = after.ExtraData };
        }

        if (fields.HasFlag(FurniEditField.Owner))
        {
            changes["owner"] = new
            {
                before = new { id = before.OwnerId.Value, name = before.OwnerName },
                after = new { id = after.OwnerId.Value, name = after.OwnerName },
            };
        }

        if (fields.HasFlag(FurniEditField.Definition))
        {
            changes["definition"] = new
            {
                before = before.DefinitionId,
                after = after.DefinitionId,
            };
        }

        await _events
            .PublishAsync(
                new ItemStaffEditedEvent(
                    before.ObjectId.Value,
                    ctx.PlayerId.Value,
                    _state.RoomId.Value,
                    fields.ToString(),
                    JsonSerializer.Serialize(changes)
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);
    }
}
