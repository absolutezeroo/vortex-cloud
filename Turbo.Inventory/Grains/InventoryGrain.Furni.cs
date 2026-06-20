using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Players;
using Turbo.Furniture;
using Turbo.Inventory.Furniture;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Events;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Furniture.Snapshots;
using Turbo.Primitives.Furniture.StuffData;
using Turbo.Primitives.Inventory.Furniture;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Snapshots.Furniture;

namespace Turbo.Inventory.Grains;

public sealed partial class InventoryGrain
{
    public async Task<bool> AddFurnitureAsync(IFurnitureItem item, CancellationToken ct)
    {
        if (!await _furniModule.AddFurnitureAsync(item, ct))
        {
            return false;
        }

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(
            this.GetPrimaryKeyLong()
        );

        await presence.OnFurnitureAddedAsync(item.GetSnapshot(), ct);

        return true;
    }

    public async Task<bool> AddFurnitureFromRoomItemSnapshotAsync(
        RoomItemSnapshot snapshot,
        CancellationToken ct
    )
    {
        IFurnitureItem item = _furnitureItemsLoader.CreateFromRoomItemSnapshot(snapshot);

        if (!await _furniModule.AddFurnitureAsync(item, ct))
        {
            return false;
        }

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(
            this.GetPrimaryKeyLong()
        );

        await presence.OnFurnitureAddedAsync(item.GetSnapshot(), ct);

        return true;
    }

    public async Task<bool> RemoveFurnitureAsync(RoomObjectId itemId, CancellationToken ct)
    {
        if (!await _furniModule.RemoveFurnitureAsync(itemId, ct))
        {
            return false;
        }

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(
            this.GetPrimaryKeyLong()
        );

        await presence.OnFurnitureRemovedAsync(itemId, ct);

        return true;
    }

    public async Task GrantCatalogOfferAsync(
        CatalogOfferSnapshot offer,
        string extraParam,
        int quantity,
        CancellationToken ct
    )
    {
        quantity = Math.Max(1, quantity);

        List<FurnitureEntity> furniEntities = new();
        List<string> badgeCodes = new();

        foreach (CatalogProductSnapshot product in offer.Products)
        {
            if (product.ProductType is ProductType.Floor || product.ProductType is ProductType.Wall)
            {
                FurnitureDefinitionSnapshot def =
                    _furnitureDefinitionProvider.TryGetDefinition(product.FurniDefinitionId)
                    ?? throw new TurboException(TurboErrorCodeEnum.FurnitureDefinitionNotFound);

                for (int i = 0; i < quantity; i++)
                {
                    furniEntities.Add(
                        new FurnitureEntity
                        {
                            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
                            FurnitureDefinitionEntityId = def.Id,
                        }
                    );
                }

                continue;
            }

            if (
                product.ProductType is ProductType.Badge
                && !string.IsNullOrWhiteSpace(product.ExtraParam)
            )
            {
                badgeCodes.Add(product.ExtraParam);
            }
        }

        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            dbCtx.AddRange(furniEntities);

            List<string> grantedBadgeCodes = new();

            foreach (string badgeCode in badgeCodes)
            {
                bool alreadyOwned = await dbCtx
                    .PlayerBadges.AnyAsync(
                        b =>
                            b.PlayerEntityId == (int)this.GetPrimaryKeyLong()
                            && b.BadgeCode == badgeCode,
                        ct
                    )
                    .ConfigureAwait(false);

                if (alreadyOwned)
                {
                    continue;
                }

                dbCtx.PlayerBadges.Add(
                    new PlayerBadgeEntity
                    {
                        PlayerEntityId = (int)this.GetPrimaryKeyLong(),
                        BadgeCode = badgeCode,
                        SlotId = 0,
                        PlayerEntity = null!,
                    }
                );

                grantedBadgeCodes.Add(badgeCode);
            }

            await dbCtx.SaveChangesAsync(ct);

            foreach (FurnitureEntity entity in furniEntities)
            {
                FurnitureDefinitionSnapshot def =
                    _furnitureDefinitionProvider.TryGetDefinition(
                        entity.FurnitureDefinitionEntityId
                    ) ?? throw new TurboException(TurboErrorCodeEnum.FurnitureDefinitionNotFound);

                await AddFurnitureAsync(
                    new FurnitureItem
                    {
                        ItemId = entity.Id,
                        OwnerId = entity.PlayerEntityId,
                        OwnerName = string.Empty,
                        Definition = def,
                        ExtraData = new ExtraData("{}"),
                        StuffData = _stuffDataFactory.CreateStuffData((int)StuffDataType.LegacyKey),
                    },
                    ct
                );

                await _events
                    .PublishAsync(
                        new ItemCreatedEvent(
                            entity.Id,
                            entity.PlayerEntityId,
                            JsonSerializer.Serialize(
                                new
                                {
                                    source = "catalog",
                                    definitionId = entity.FurnitureDefinitionEntityId,
                                }
                            )
                        ),
                        ct
                    )
                    .ConfigureAwait(false);
            }

            IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(
                this.GetPrimaryKeyLong()
            );

            foreach (string badgeCode in grantedBadgeCodes)
            {
                await presence.OnBadgeGrantedAsync(badgeCode, ct);
            }
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task GrantBadgeAsync(string badgeCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(badgeCode))
        {
            return;
        }

        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            bool alreadyOwned = await dbCtx
                .PlayerBadges.AnyAsync(
                    b =>
                        b.PlayerEntityId == (int)this.GetPrimaryKeyLong()
                        && b.BadgeCode == badgeCode,
                    ct
                )
                .ConfigureAwait(false);

            if (alreadyOwned)
            {
                return;
            }

            dbCtx.PlayerBadges.Add(
                new PlayerBadgeEntity
                {
                    PlayerEntityId = (int)this.GetPrimaryKeyLong(),
                    BadgeCode = badgeCode,
                    SlotId = 0,
                    PlayerEntity = null!,
                }
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(
                this.GetPrimaryKeyLong()
            );

            await presence.OnBadgeGrantedAsync(badgeCode, ct).ConfigureAwait(false);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task GrantFurnitureDefinitionAsync(
        int definitionId,
        string? extraData,
        CancellationToken ct
    )
    {
        FurnitureDefinitionSnapshot def =
            _furnitureDefinitionProvider.TryGetDefinition(definitionId)
            ?? throw new TurboException(TurboErrorCodeEnum.FurnitureDefinitionNotFound);

        FurnitureEntity entity = new()
        {
            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
            FurnitureDefinitionEntityId = def.Id,
            ExtraData = extraData,
        };

        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            dbCtx.Add(entity);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            await AddFurnitureAsync(
                    new FurnitureItem
                    {
                        ItemId = entity.Id,
                        OwnerId = entity.PlayerEntityId,
                        OwnerName = string.Empty,
                        Definition = def,
                        ExtraData = new ExtraData(extraData ?? "{}"),
                        StuffData = _stuffDataFactory.CreateStuffData(StuffDataType.LegacyKey),
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task GrantLtdFurnitureAsync(
        int furniDefinitionId,
        int serialNumber,
        int seriesSize,
        CancellationToken ct
    )
    {
        FurnitureDefinitionSnapshot def =
            _furnitureDefinitionProvider.TryGetDefinition(furniDefinitionId)
            ?? throw new TurboException(TurboErrorCodeEnum.FurnitureDefinitionNotFound);

        string extraData = $"{{\"serial\":{serialNumber},\"seriesSize\":{seriesSize}}}";

        FurnitureEntity entity = new()
        {
            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
            FurnitureDefinitionEntityId = def.Id,
            ExtraData = extraData,
        };

        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            dbCtx.Add(entity);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            await AddFurnitureAsync(
                    new FurnitureItem
                    {
                        ItemId = entity.Id,
                        OwnerId = entity.PlayerEntityId,
                        OwnerName = string.Empty,
                        Definition = def,
                        ExtraData = new ExtraData(extraData),
                        StuffData = _stuffDataFactory.CreateStuffData(StuffDataType.LegacyKey),
                    },
                    ct
                )
                .ConfigureAwait(false);

            await _events
                .PublishAsync(
                    new ItemCreatedEvent(
                        entity.Id,
                        entity.PlayerEntityId,
                        JsonSerializer.Serialize(
                            new
                            {
                                source = "ltd",
                                serial = serialNumber,
                                seriesSize,
                            }
                        )
                    ),
                    ct
                )
                .ConfigureAwait(false);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }

    public Task<FurnitureItemSnapshot?> GetItemSnapshotAsync(
        RoomObjectId itemId,
        CancellationToken ct
    )
    {
        return _furniModule.GetItemSnapshotAsync(itemId, ct);
    }

    public Task<ImmutableArray<FurnitureItemSnapshot>> GetAllItemSnapshotsAsync(
        CancellationToken ct
    )
    {
        return _furniModule.GetAllItemSnapshotsAsync(ct);
    }

    public Task EnsureFurnitureReadyAsync(CancellationToken ct)
    {
        return _furniModule.EnsureFurnitureReadyAsync(ct);
    }
}
