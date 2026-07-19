using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Object.Furniture.Wall;

namespace Vortex.Rooms.Providers;

internal sealed class RoomItemsProvider(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<IRoomItemsProvider> logger,
    IGrainFactory grainFactory,
    IFurnitureDefinitionProvider defsProvider
) : IRoomItemsProvider
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IRoomItemsProvider> _logger = logger;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IFurnitureDefinitionProvider _defsProvider = defsProvider;

    public async Task<(
        IReadOnlyList<IRoomFloorItem>,
        IReadOnlyList<IRoomWallItem>,
        IReadOnlyDictionary<PlayerId, string>
    )> LoadByRoomIdAsync(RoomId roomId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<FurnitureEntity> entities = await dbCtx
            .Furnitures.AsNoTracking()
            .Where(x => x.RoomEntityId == (int)roomId && x.DeletedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<IRoomFloorItem> floorItems = new List<IRoomFloorItem>();
        List<IRoomWallItem> wallItems = new List<IRoomWallItem>();

        List<PlayerId> ownerIdsUnique = entities
            .Select(x => (PlayerId)x.PlayerEntityId)
            .Distinct()
            .ToList();
        ImmutableDictionary<PlayerId, string> ownerNames = await _grainFactory
            .GetPlayerDirectoryGrain()
            .GetPlayerNamesAsync(ownerIdsUnique, ct)
            .ConfigureAwait(false);

        foreach (FurnitureEntity entity in entities)
        {
            try
            {
                IRoomItem item = CreateFromEntity(entity);

                item.SetExtraData(entity.ExtraData);

                item.SetOwnerName(
                    ownerNames.TryGetValue(entity.PlayerEntityId, out string? name)
                        ? name ?? string.Empty
                        : string.Empty
                );

                item.SetPosition(entity.X, entity.Y);
                item.SetPositionZ(entity.Z);
                item.SetRotation(entity.Rotation);

                if (item is IRoomFloorItem floorItem)
                {
                    floorItems.Add(floorItem);
                }
                else if (item is IRoomWallItem wallItem)
                {
                    wallItem.SetWallOffset(entity.WallOffset);

                    wallItems.Add(wallItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to load furniture {ItemId} in room {RoomId}; skipping item.",
                    entity.Id,
                    roomId
                );
            }
        }

        return (floorItems, wallItems, ownerNames);
    }

    public IRoomItem CreateFromEntity(FurnitureEntity entity)
    {
        FurnitureDefinitionSnapshot definition =
            _defsProvider.TryGetDefinition(entity.FurnitureDefinitionEntityId)
            ?? throw new VortexException(VortexErrorCodeEnum.FurnitureDefinitionNotFound);

        return definition.ProductType switch
        {
            ProductType.Floor => new RoomFloorItem
            {
                ObjectId = entity.Id,
                OwnerId = entity.PlayerEntityId,
                OwnerName = string.Empty,
                Definition = definition,
            },

            ProductType.Wall => new RoomWallItem
            {
                ObjectId = entity.Id,
                OwnerId = entity.PlayerEntityId,
                OwnerName = string.Empty,
                Definition = definition,
            },

            _ => throw new VortexException(VortexErrorCodeEnum.InvalidFurnitureProductType),
        };
    }

    public IRoomItem CreateFromFurnitureItemSnapshot(FurnitureItemSnapshot snapshot)
    {
        FurnitureDefinitionSnapshot definition = snapshot.Definition;

        IRoomItem? item = null;

        if (definition.ProductType == ProductType.Floor)
        {
            item = new RoomFloorItem
            {
                ObjectId = snapshot.ItemId,
                OwnerId = snapshot.OwnerId,
                OwnerName = snapshot.OwnerName,
                Definition = definition,
            };
        }

        if (definition.ProductType == ProductType.Wall)
        {
            item = new RoomWallItem
            {
                ObjectId = snapshot.ItemId,
                OwnerId = snapshot.OwnerId,
                OwnerName = snapshot.OwnerName,
                Definition = definition,
            };
        }

        if (item is null)
        {
            throw new VortexException(VortexErrorCodeEnum.InvalidFurnitureProductType);
        }

        item.SetExtraData(snapshot.ExtraData);

        return item;
    }
}
