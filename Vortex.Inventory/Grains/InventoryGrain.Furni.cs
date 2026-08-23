using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Players;
using Vortex.Furniture;
using Vortex.Inventory.Furniture;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Groups;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Inventory.Furniture;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Bots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Inventory.Grains;

public sealed partial class InventoryGrain
{
    /// <summary>
    /// Rebuilds the cached furniture list from the database, then tells the client to ask for it
    /// again.
    /// </summary>
    /// <remarks>
    /// The notification is the point. This exists for the wired chest settlements, which move rows
    /// by writing <c>WiredChestEntityId</c> straight through EF rather than going through
    /// <see cref="AddFurnitureAsync"/> -- so nothing on either side ever reached the client, and a
    /// deposited item sat in the player's inventory looking like it had never left. Reloading only
    /// fixed the grain's own copy, which is the half nobody can see.
    /// </remarks>
    public async Task ReloadFurnitureAsync(CancellationToken ct)
    {
        await _furniModule.ReloadAsync(ct);

        await _grainFactory
            .GetPlayerPresenceGrain(this.GetPrimaryKeyLong())
            .OnFurnitureListInvalidatedAsync(ct);
    }

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
        await RefreshMysteryBoxTrackerAsync(item.Definition.Id, ct).ConfigureAwait(true);

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
        // Read the definition before the item is gone: the mystery box tracker is derived from the
        // boxes a player owns, so losing one has to refresh it and afterwards there is nothing left
        // to look the definition up from.
        FurnitureItemSnapshot? leaving = await _furniModule
            .GetItemSnapshotAsync(itemId, ct)
            .ConfigureAwait(true);

        if (!await _furniModule.RemoveFurnitureAsync(itemId, ct))
        {
            return false;
        }

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(
            this.GetPrimaryKeyLong()
        );

        await presence.OnFurnitureRemovedAsync(itemId, ct);

        if (leaving is not null)
        {
            await RefreshMysteryBoxTrackerAsync(leaving.Definition.Id, ct).ConfigureAwait(true);
        }

        return true;
    }

    /// <summary>
    /// Re-pushes the mystery box toolbar tracker when the furniture that just entered or left the
    /// inventory is a registered box. The tracker shows the colour of a box the player owns, so a
    /// catalogue purchase, a trade or a staff grant all change it — without this it would only
    /// correct itself on the next login. Gated on the definition actually being a box, so every
    /// other furniture movement costs one cached dictionary lookup and nothing else.
    /// </summary>
    private async Task RefreshMysteryBoxTrackerAsync(int definitionId, CancellationToken ct)
    {
        bool isBox = await _grainFactory
            .GetMysteryBoxManagerGrain()
            .IsBoxDefinitionAsync(definitionId, ct)
            .ConfigureAwait(true);

        if (!isBox)
        {
            return;
        }

        await _grainFactory
            .GetPlayerMysteryBoxGrain(this.GetPrimaryKeyLong())
            .PushTrackerAsync(ct)
            .ConfigureAwait(true);
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
        List<PetCreateRequest> petRequests = new();
        List<BotCreateRequest> botRequests = new();
        List<(int EffectId, int SubType, int Duration)> effectGrants = new();

        // Guild furni is bought from the guild pages with the guild id in extraParam; the badge and
        // both recolours have to be baked into the item's stuff data at grant time, because the
        // client renders them straight from there and never asks the server for them again.
        GuildFurniIdentitySnapshot? guildIdentity =
            int.TryParse(extraParam, out int guildId) && guildId > 0
                ? await _grainFactory
                    .GetGroupDirectoryGrain()
                    .GetFurniIdentityAsync((int)this.GetPrimaryKeyLong(), guildId, ct)
                    .ConfigureAwait(true)
                : null;

        foreach (CatalogProductSnapshot product in offer.Products)
        {
            if (product.ProductType is ProductType.Floor || product.ProductType is ProductType.Wall)
            {
                FurnitureDefinitionSnapshot def =
                    _furnitureDefinitionProvider.TryGetDefinition(product.FurniDefinitionId)
                    ?? throw new VortexException(VortexErrorCodeEnum.FurnitureDefinitionNotFound);

                // Only string-array furni can carry the guild layout; stamping anything else would
                // corrupt its own stuff data.
                string? guildExtraData =
                    guildIdentity is not null && def.StuffDataType == StuffDataType.StringKey
                        ? BuildGuildExtraData(guildIdentity)
                        : null;

                // Each product carries its own per-offer count (a bundle is an offer holding
                // several products, and any product may bundle >1 of an item), so the copies to
                // grant are the purchase multiplier times that count. Ignoring product.Quantity
                // collapsed every bundle to one of each item.
                int copies = quantity * Math.Max(1, product.Quantity);

                for (int i = 0; i < copies; i++)
                {
                    furniEntities.Add(
                        new FurnitureEntity
                        {
                            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
                            FurnitureDefinitionEntityId = def.Id,
                            ExtraData = guildExtraData,
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
                continue;
            }

            if (
                product.ProductType is ProductType.Effect
                && !string.IsNullOrWhiteSpace(product.ExtraParam)
            )
            {
                // ExtraParam encodes the effect: "effectId", "effectId:durationSeconds", or
                // "effectId:durationSeconds:subType" (duration 0/absent = permanent). One grant per copy.
                string[] fx = product.ExtraParam.Split(':');

                if (int.TryParse(fx[0], out int effectId) && effectId > 0)
                {
                    int duration = fx.Length > 1 && int.TryParse(fx[1], out int d) ? d : 0;
                    int subType = fx.Length > 2 && int.TryParse(fx[2], out int s) ? s : 0;
                    int copies = quantity * Math.Max(1, product.Quantity);

                    for (int i = 0; i < copies; i++)
                    {
                        effectGrants.Add((effectId, subType, duration));
                    }
                }

                continue;
            }

            if (product.ProductType is ProductType.Robot)
            {
                BotCreateRequest? bot = TryReadBotProduct(product.ExtraParam, extraParam);

                if (bot is null)
                {
                    _logger.LogWarning(
                        "Catalog product {ProductId} is a Robot but its extra param '{ExtraParam}' carries no figure; skipping the grant.",
                        product.Id,
                        product.ExtraParam
                    );

                    continue;
                }

                botRequests.Add(bot);

                continue;
            }

            if (product.ProductType is ProductType.Pet)
            {
                _ = int.TryParse(product.ExtraParam, out int petType);

                string[] parts = extraParam.Split('\n');
                string petName = parts.Length > 0 ? parts[0].Trim() : "Pet";
                int race = parts.Length > 1 && int.TryParse(parts[1], out int r) ? r : 0;
                string color = parts.Length > 2 ? parts[2].Trim() : "ffffff";

                if (string.IsNullOrWhiteSpace(petName))
                {
                    petName = "Pet";
                }

                petRequests.Add(
                    new PetCreateRequest
                    {
                        Name = petName,
                        Type = petType,
                        Race = race,
                        Color = color,
                        Gender = AvatarGenderType.Male,
                        Energy = 100,
                        Nutrition = 100,
                    }
                );
            }
        }

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

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
                    .ConfigureAwait(true);

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
                    ) ?? throw new VortexException(VortexErrorCodeEnum.FurnitureDefinitionNotFound);

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
                    .ConfigureAwait(true);
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
            await dbCtx.DisposeAsync().ConfigureAwait(true);
        }

        if (petRequests.Count > 0)
        {
            IPlayerPresenceGrain petPresence = _grainFactory.GetPlayerPresenceGrain(
                this.GetPrimaryKeyLong()
            );

            foreach (PetCreateRequest req in petRequests)
            {
                PetSnapshot pet = await CreatePetAsync(req, ct).ConfigureAwait(true);

                await petPresence.OnPetAddedToInventoryAsync(pet, ct).ConfigureAwait(true);
            }
        }

        if (botRequests.Count > 0)
        {
            IPlayerPresenceGrain botPresence = _grainFactory.GetPlayerPresenceGrain(
                this.GetPrimaryKeyLong()
            );

            foreach (BotCreateRequest req in botRequests)
            {
                BotSnapshot bot = await CreateBotAsync(req, ct).ConfigureAwait(true);

                // Opens the inventory on top of adding the row: the player just bought this, so
                // showing them where it went is the point.
                await botPresence
                    .SendComposerAsync(
                        new BotAddedToInventoryEventMessageComposer
                        {
                            Bot = bot,
                            OpenInventory = true,
                        }
                    )
                    .ConfigureAwait(true);
            }
        }

        if (effectGrants.Count > 0)
        {
            // The effect grain owns the player_effects table and pushes AvatarEffectAdded itself. A throw
            // here propagates to the wallet's ExecutePurchaseAsync so the purchase auto-refunds.
            IPlayerEffectGrain effects = _grainFactory.GetPlayerEffectGrain(
                this.GetPrimaryKeyLong()
            );

            foreach ((int effectId, int subType, int duration) in effectGrants)
            {
                await effects.AddEffectAsync(effectId, subType, duration, ct).ConfigureAwait(true);
            }
        }
    }

    /// <summary>
    /// Reads a bot product's definition. Habbo writes these as semicolon-separated key:value pairs
    /// — <c>name:Robbie;figure:hd-180-1...;gender:m;motto:...</c> — and a figure string itself
    /// contains neither separator, so the keys are unambiguous.
    /// <para>
    /// A bare figure with no keys is also accepted, because that is what a hand-written product
    /// looks like and rejecting it would be a trap rather than a rule.
    /// </para>
    /// </summary>
    /// <returns>Null when no figure could be found, which is the one field a bot cannot do without.</returns>
    internal static BotCreateRequest? TryReadBotProduct(
        string? productExtraParam,
        string purchaseExtraParam
    )
    {
        string definition = productExtraParam ?? string.Empty;

        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase);
        string? bareFigure = null;

        foreach (string part in definition.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = part.IndexOf(':', StringComparison.Ordinal);

            if (separator <= 0)
            {
                bareFigure ??= part.Trim();
                continue;
            }

            fields[part[..separator].Trim()] = part[(separator + 1)..].Trim();
        }

        string figure = fields.GetValueOrDefault("figure", bareFigure ?? string.Empty);

        if (string.IsNullOrWhiteSpace(figure))
        {
            return null;
        }

        // The product names the bot; Habbo does not ask the buyer for one the way it does for a
        // pet. A typed name is still honoured if the product left the field out.
        string typedName = purchaseExtraParam.Split('\n')[0].Trim();
        string name = fields.GetValueOrDefault("name", string.Empty);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = string.IsNullOrWhiteSpace(typedName) ? "Bot" : typedName;
        }

        return new BotCreateRequest
        {
            Name = name,
            Figure = figure,
            Gender = fields
                .GetValueOrDefault("gender", "m")
                .StartsWith("f", StringComparison.OrdinalIgnoreCase)
                ? AvatarGenderType.Female
                : AvatarGenderType.Male,
            Motto = fields.GetValueOrDefault("motto", string.Empty),
        };
    }

    public async Task GrantBadgeAsync(string badgeCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(badgeCode))
        {
            return;
        }

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            bool alreadyOwned = await dbCtx
                .PlayerBadges.AnyAsync(
                    b =>
                        b.PlayerEntityId == (int)this.GetPrimaryKeyLong()
                        && b.BadgeCode == badgeCode,
                    ct
                )
                .ConfigureAwait(true);

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

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(
                this.GetPrimaryKeyLong()
            );

            await presence.OnBadgeGrantedAsync(badgeCode, ct).ConfigureAwait(true);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
        }
    }

    public async Task RemoveBadgeAsync(string badgeCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(badgeCode))
        {
            return;
        }

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            PlayerBadgeEntity? badge = await dbCtx
                .PlayerBadges.FirstOrDefaultAsync(
                    b =>
                        b.PlayerEntityId == (int)this.GetPrimaryKeyLong()
                        && b.BadgeCode == badgeCode,
                    ct
                )
                .ConfigureAwait(true);

            if (badge is null)
            {
                return;
            }

            dbCtx.PlayerBadges.Remove(badge);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
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
            ?? throw new VortexException(VortexErrorCodeEnum.FurnitureDefinitionNotFound);

        FurnitureEntity entity = new()
        {
            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
            FurnitureDefinitionEntityId = def.Id,
            ExtraData = extraData,
        };

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            dbCtx.Add(entity);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            await AddFurnitureAsync(
                    new FurnitureItem
                    {
                        ItemId = entity.Id,
                        OwnerId = entity.PlayerEntityId,
                        OwnerName = string.Empty,
                        Definition = def,
                        ExtraData = new ExtraData(extraData ?? "{}"),
                        // Built from the stored blob, exactly like InventoryFurnitureLoader does on
                        // login: a blank legacy default would drop whatever the grant baked in (a
                        // guild badge, a trophy inscription) until the player next reconnected.
                        StuffData = _stuffDataFactory.CreateStuffDataFromJson(
                            def.StuffDataType,
                            extraData
                        ),
                    },
                    ct
                )
                .ConfigureAwait(true);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
        }
    }

    public async Task GrantFurnitureWithLegacyStuffDataAsync(
        int definitionId,
        string legacyData,
        CancellationToken ct
    )
    {
        ExtraData extraData = new(null);

        extraData.UpdateSection(ExtraDataSectionType.STUFF, new { Data = legacyData });

        await GrantFurnitureDefinitionAsync(definitionId, extraData.GetJsonString(), ct)
            .ConfigureAwait(true);
    }

    public async Task<FurnitureItemSnapshot?> GrantSingleFurnitureIfUnderLimitAsync(
        int definitionId,
        string? extraData,
        int furniLimit,
        CancellationToken ct
    )
    {
        ImmutableArray<FurnitureItemSnapshot> owned = await _furniModule
            .GetAllItemSnapshotsAsync(ct)
            .ConfigureAwait(true);

        if (owned.Length >= furniLimit)
        {
            return null;
        }

        FurnitureDefinitionSnapshot def =
            _furnitureDefinitionProvider.TryGetDefinition(definitionId)
            ?? throw new VortexException(VortexErrorCodeEnum.FurnitureDefinitionNotFound);

        FurnitureEntity entity = new()
        {
            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
            FurnitureDefinitionEntityId = def.Id,
            ExtraData = extraData,
        };

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            dbCtx.Add(entity);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
        }

        FurnitureItem item = new()
        {
            ItemId = entity.Id,
            OwnerId = entity.PlayerEntityId,
            OwnerName = string.Empty,
            Definition = def,
            ExtraData = new ExtraData(extraData ?? "{}"),
            StuffData = _stuffDataFactory.CreateStuffData(StuffDataType.LegacyKey),
        };

        await AddFurnitureAsync(item, ct).ConfigureAwait(true);

        return item.GetSnapshot();
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
            ?? throw new VortexException(VortexErrorCodeEnum.FurnitureDefinitionNotFound);

        string extraData = $"{{\"serial\":{serialNumber},\"seriesSize\":{seriesSize}}}";

        FurnitureEntity entity = new()
        {
            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
            FurnitureDefinitionEntityId = def.Id,
            ExtraData = extraData,
        };

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            dbCtx.Add(entity);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

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
                .ConfigureAwait(true);

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
                .ConfigureAwait(true);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
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

    /// <summary>
    /// Puts a bought offer in a box instead of in the recipient's hands: one present furniture whose
    /// private section names the offer, to be granted for real when they unwrap it.
    /// </summary>
    /// <returns>False when the chosen wrapping names no furniture this hotel ships, so the caller
    /// can fall back to handing the offer over unwrapped rather than swallowing a paid purchase.</returns>
    public async Task<bool> GrantWrappedGiftAsync(
        CatalogOfferSnapshot offer,
        string extraParam,
        GiftWrappingSpec wrapping,
        string purchaserName,
        string purchaserFigure,
        CancellationToken ct
    )
    {
        FurnitureDefinitionSnapshot? present = ResolvePresentDefinition(wrapping.StuffTypeId);

        if (present is null)
        {
            _logger.LogWarning(
                "Gift wrapping stuff type {StuffType} resolves to no present definition; the gift will be granted unwrapped.",
                wrapping.StuffTypeId
            );

            return false;
        }

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            dbCtx.Add(
                new FurnitureEntity
                {
                    PlayerEntityId = (int)this.GetPrimaryKeyLong(),
                    FurnitureDefinitionEntityId = present.Id,
                    ExtraData = BuildPresentExtraData(
                        offer,
                        extraParam,
                        wrapping,
                        purchaserName,
                        purchaserFigure
                    ),
                }
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
        }

        await _furniModule.ReloadAsync(ct).ConfigureAwait(true);

        return true;
    }

    /// <summary>
    /// The wrapping's furniture. Stuff type 1 is the plain <c>present_gen</c> and 2-7 are its
    /// numbered variants — the client offers seven and names them by index, not by classname.
    /// </summary>
    private FurnitureDefinitionSnapshot? ResolvePresentDefinition(int stuffTypeId)
    {
        int index = Math.Clamp(stuffTypeId, 1, 7) - 1;
        string name = index == 0 ? "present_gen" : $"present_gen{index}";

        return _furnitureDefinitionProvider.TryGetDefinitionByName(name);
    }

    /// <summary>
    /// Both halves of a present in one blob: the "stuff" section every client in the room can read,
    /// and the private section only the server does.
    /// </summary>
    private static string BuildPresentExtraData(
        CatalogOfferSnapshot offer,
        string extraParam,
        GiftWrappingSpec wrapping,
        string purchaserName,
        string purchaserFigure
    )
    {
        ExtraData extraData = new(null);

        // Key names are the client's, from FurniturePresentLogic.setObjectVariables. An anonymous
        // gift omits the sender entirely rather than sending an empty string, because the widget
        // draws whatever it is given.
        Dictionary<string, string> data = new()
        {
            ["MESSAGE"] = wrapping.Message,
            ["PRODUCT_CODE"] =
                offer.Products.Length > 0
                    ? offer.Products[0].FurniDefinitionId.ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
            ["TRUSTED_SENDER"] = "false",
        };

        if (wrapping.ShowPurchaserName)
        {
            data["PURCHASER_NAME"] = purchaserName;
            data["PURCHASER_FIGURE"] = purchaserFigure;
        }

        extraData.UpdateSection(ExtraDataSectionType.STUFF, new { Data = data });
        extraData.UpdateSection(
            ExtraDataSectionType.PRESENT,
            new PresentContentsSnapshot
            {
                OfferId = offer.Id,
                ExtraParam = extraParam,
                Wrapping = FurniturePresentWrapping.Pack(wrapping.BoxTypeId, wrapping.RibbonTypeId),
            }
        );

        return extraData.GetJsonString();
    }

    /// <summary>
    /// Serializes the guild layout into the item's extra-data blob, in the same "stuff" section
    /// shape <see cref="Vortex.Furniture.Providers.StuffDataFactory"/> reads back.
    /// </summary>
    private static string BuildGuildExtraData(GuildFurniIdentitySnapshot identity)
    {
        ExtraData extraData = new(null);

        extraData.UpdateSection(
            ExtraDataSectionType.STUFF,
            new
            {
                Data = GuildFurniStuffData.Build(
                    identity.GroupId,
                    identity.BadgeCode,
                    identity.ColorOneHex,
                    identity.ColorTwoHex
                ),
            }
        );

        return extraData.GetJsonString();
    }
}
