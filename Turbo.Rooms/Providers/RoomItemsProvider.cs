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
using Turbo.Database.Context;
using Turbo.Database.Entities.Furniture;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Furniture.Snapshots;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Furniture.Wall;
using Turbo.Primitives.Rooms.Providers;
using Turbo.Rooms.Object.Furniture.Floor;
using Turbo.Rooms.Object.Furniture.Wall;

namespace Turbo.Rooms.Providers;

internal sealed class RoomItemsProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<IRoomItemsProvider> logger,
    IGrainFactory grainFactory,
    IFurnitureDefinitionProvider defsProvider
) : IRoomItemsProvider
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IRoomItemsProvider> _logger = logger;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IFurnitureDefinitionProvider _defsProvider = defsProvider;

    public async Task<(
        IReadOnlyList<IRoomFloorItem>,
        IReadOnlyList<IRoomWallItem>,
        IReadOnlyDictionary<PlayerId, string>
    )> LoadByRoomIdAsync(RoomId roomId, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
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
            catch (Exception)
            {
                continue;
            }
        }

        return (floorItems, wallItems, ownerNames);
    }

    public IRoomItem CreateFromEntity(FurnitureEntity entity)
    {
        FurnitureDefinitionSnapshot definition =
            _defsProvider.TryGetDefinition(entity.FurnitureDefinitionEntityId)
            ?? throw new TurboException(TurboErrorCodeEnum.FurnitureDefinitionNotFound);

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

            _ => throw new TurboException(TurboErrorCodeEnum.InvalidFurnitureProductType),
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
            throw new TurboException(TurboErrorCodeEnum.InvalidFurnitureProductType);
        }

        item.SetExtraData(snapshot.ExtraData);

        return item;
    }
}
