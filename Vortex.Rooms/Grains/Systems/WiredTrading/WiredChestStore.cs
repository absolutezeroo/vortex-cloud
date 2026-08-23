using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Wired;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Grains.Systems.WiredTrading;

/// <summary>
/// The chest rows themselves: settings, stock, and the stuff data the client's furni reads.
/// </summary>
/// <remarks>
/// The single gateway to <c>wired_chests</c> and to the furniture parked in one. Everything that
/// used to look a chest up by furni id in a dozen places, and open a row for it in four, comes
/// through here instead -- a chest that exists twice is a chest whose contents disagree.
/// <para>
/// Written only by the activation of the room the furni stands in, which is what makes the
/// read-modify-write below safe without a transaction.
/// </para>
/// </remarks>
public sealed class WiredChestStore(RoomGrain roomGrain, RoomWiredTradingSystem system)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    /// <summary>Whether a chest is being looked into is a question about screens, not about
    /// rows, and the answer changes what the furni is drawn as.</summary>
    private readonly RoomWiredTradingSystem _system = system;

    /// <summary>
    /// Which half a chest stores, decided on its logic rather than its classname.
    /// </summary>
    /// <remarks>
    /// A classname is not a key here: furniture_definitions holds thousands of duplicates and the
    /// furnidata itself ships them, so a prefix match on the name is a guess that happens to work.
    /// The logic is the binding the room already resolves the furni through, it is one value per
    /// behaviour, and it is what decides whether the object is a chest at all.
    /// </remarks>
    private const string CoinChestLogic = "furniture_coinschest";

    private const string FurniChestLogic = "furniture_furnichest";

    internal static bool IsCoinChestLogic(string logicName) =>
        string.Equals(logicName, CoinChestLogic, StringComparison.Ordinal);

    /// <summary>
    /// The chest's row as it stands, tracked by the caller's context, or null when nobody has ever
    /// touched it.
    /// </summary>
    /// <remarks>
    /// Takes the caller's context rather than opening one: a chest read here is usually about to be
    /// written beside the furniture it holds, and two contexts cannot be saved as one movement.
    /// </remarks>
    internal static Task<WiredChestEntity?> FindAsync(
        VortexDbContext dbCtx,
        int chestId,
        CancellationToken ct
    ) =>
        dbCtx.WiredChests.FirstOrDefaultAsync(
            c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
            ct
        );

    /// <summary>The same row, for a caller that only means to read it.</summary>
    internal static Task<WiredChestEntity?> ReadAsync(
        VortexDbContext dbCtx,
        int chestId,
        CancellationToken ct
    ) =>
        dbCtx
            .WiredChests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null, ct);

    /// <summary>
    /// The chest's row, opened on first use.
    /// </summary>
    /// <remarks>
    /// A chest's row is created the first time something needs one rather than when the furni is
    /// placed: a chest nobody has ever touched holds nothing, and a row saying so is a row to keep
    /// in step for nothing.
    /// <para>
    /// Saved immediately on creation, because what follows a call to this is almost always a query
    /// against <c>chest.Id</c> — the stock parked in it — and an entity that has not been inserted
    /// has no id to match. This was written out four times, once per caller, and a fifth caller
    /// would have written it a fifth.
    /// </para>
    /// </remarks>
    internal static async Task<WiredChestEntity> GetOrOpenAsync(
        VortexDbContext dbCtx,
        int chestId,
        CancellationToken ct
    )
    {
        WiredChestEntity? chest = await FindAsync(dbCtx, chestId, ct).ConfigureAwait(true);

        if (chest is not null)
        {
            return chest;
        }

        chest = new WiredChestEntity
        {
            FurnitureEntityId = chestId,
            Credits = 0,
            NotificationsEnabled = true,
        };

        dbCtx.WiredChests.Add(chest);

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        return chest;
    }

    internal static bool IsChestLogic(string logicName) =>
        string.Equals(logicName, CoinChestLogic, StringComparison.Ordinal)
        || string.Equals(logicName, FurniChestLogic, StringComparison.Ordinal);

    /// <summary>Whether this player may operate that chest: it has to be a chest, standing in this
    /// room, and they have to be allowed to decorate.</summary>
    internal async Task<bool> CanUseChestAsync(ActionContext ctx, int chestId)
    {
        if (
            ctx.PlayerId <= 0
            || !_roomGrain._state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !IsChestLogic(item.Definition.LogicName)
        )
        {
            return false;
        }

        RoomControllerType level = await _roomGrain
            .SecurityModule.GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        return level != RoomControllerType.None;
    }

    internal Task<WiredChestSnapshot?> SnapshotAsync(int chestId, int credits) =>
        Task.FromResult<WiredChestSnapshot?>(
            _roomGrain._state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
                ? new WiredChestSnapshot
                {
                    ChestId = chestId,
                    Credits = credits,
                    IsCoinChest = IsCoinChestLogic(item.Definition.LogicName),
                }
                : null
        );

    /// <summary>Turns a stored row into the item shape the chest screen and the inventory both
    /// speak. Owner name is left empty on purpose: the chest screen never shows it, and resolving a
    /// name per row would cost a lookup per item for nothing.</summary>
    internal FurnitureItemSnapshot? ToChestItemSnapshot(FurnitureEntity entity)
    {
        FurnitureDefinitionSnapshot? definition = _roomGrain._definitionProvider.TryGetDefinition(
            entity.FurnitureDefinitionEntityId
        );

        if (definition is null)
        {
            return null;
        }

        return new FurnitureItemSnapshot
        {
            ItemId = entity.Id,
            SpriteId = definition.SpriteId,
            OwnerId = entity.PlayerEntityId,
            OwnerName = string.Empty,
            Definition = definition,
            StuffData = _roomGrain
                ._stuffDataFactory.CreateStuffDataFromJson(
                    definition.StuffDataType,
                    entity.ExtraData
                )
                .GetSnapshot(),
            ExtraData = entity.ExtraData ?? string.Empty,
            SecondsToExpiration = -1,
            HasRentPeriodStarted = false,
            RoomId = -1,
        };
    }

    /// <summary>The kind the client names when it asks for "three of these": sprite and wall/floor,
    /// plus the poster number when the kind is a poster. It is the same expression the serializer
    /// uses to fill that field, so whatever we sent comes back matching.</summary>
    internal static bool IsSameKind(
        FurnitureItemSnapshot item,
        bool isWallItem,
        int typeId,
        string legacyPosterId
    ) =>
        item.SpriteId == typeId
        && (item.Definition.ProductType == ProductType.Wall) == isWallItem
        && (
            item.Definition.FurniCategory != FurnitureCategory.Poster
            || item.ExtraData == legacyPosterId
        );

    /// <summary>
    /// Copies a chest's saved settings onto the furni the client reads them from.
    /// </summary>
    /// <remarks>
    /// The dialogs prefill from the furni's map stuff data, never from a message, so a chest whose
    /// settings live only in the table shows a blank screen. The keys themselves are
    /// <see cref="WiredChestStuffData" />'s business, not this grain's.
    /// </remarks>
    internal async Task ApplyChestSettingsToStuffDataAsync(int chestId, WiredChestEntity chest)
    {
        if (
            !_roomGrain._state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || item.Logic.StuffData is not IMapStuffData map
        )
        {
            return;
        }

        WiredChestStuffData.Apply(map, chest);

        bool? open = ResolveChestOpenState(chest, _system.IsBeingLookedInto(chestId));

        if (open is not null)
        {
            WiredChestStuffData.ApplyState(map, open.Value);
        }

        WiredChestStuffData.ApplyPreview(
            map,
            BuildChestPreview(chest, await ReadChestItemsAsync(chest).ConfigureAwait(true))
        );

        await item.Logic.PersistStuffDataAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// The state the chest should wear, or null to leave whatever it is wearing alone.
    /// </summary>
    /// <remarks>
    /// The four values are the client's own dropdown, in its order: open when looked into, always
    /// open, always closed, controlled by Wired. Only the last one is not ours to decide.
    /// </remarks>
    private static bool? ResolveChestOpenState(WiredChestEntity chest, bool beingLookedInto) =>
        chest.ChestState switch
        {
            0 => beingLookedInto,
            1 => true,
            2 => false,
            _ => null,
        };

    /// <summary>The chest's contents, as the kinds a preview is picked from.</summary>
    private async Task<List<ChestPreviewKind>> ReadChestItemsAsync(WiredChestEntity chest)
    {
        // A chest with no preview to draw is the common case; do not go to the database for it.
        if (chest.PreviewItems == 0)
        {
            return [];
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(CancellationToken.None)
            .ConfigureAwait(true);

        List<FurnitureEntity> stored = await dbCtx
            .Furnitures.AsNoTracking()
            .Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
            // Row id stands in for "when it entered the chest", which no column records: nothing
            // stamps a furni on the way in. Only the two "recent"/"oldest" modes read the order.
            .OrderBy(f => f.Id)
            .ToListAsync(CancellationToken.None)
            .ConfigureAwait(true);

        return
        [
            .. stored
                .Select(ToChestItemSnapshot)
                .OfType<FurnitureItemSnapshot>()
                .Select(item => new ChestPreviewKind(
                    item.Definition.ProductType == ProductType.Wall,
                    item.SpriteId,
                    item.Definition.FurniCategory == FurnitureCategory.Poster
                        ? item.ExtraData
                        : string.Empty
                )),
        ];
    }

    /// <summary>Picks what an open chest shows, from the owner's two appearance settings.</summary>
    /// <remarks>
    /// The modes are the client's dropdown, and the starred ones ("Random items (*)") are the same
    /// order with duplicate kinds pushed to the back — the client's own note explains them as
    /// "prefer to show different item types", not "show only different item types", so a chest
    /// holding one kind still fills its slots with it.
    /// <para>
    /// Mode 7, "next-in-line random items to be given through Wired", is deliberately not
    /// implemented: it names a queue of upcoming Wired rewards that nothing here keeps. It shows
    /// nothing rather than showing something else and calling it that.
    /// </para>
    /// </remarks>
    private static List<ChestPreviewKind> BuildChestPreview(
        WiredChestEntity chest,
        List<ChestPreviewKind> items
    )
    {
        if (items.Count == 0 || chest.PreviewItems is 0 or 7)
        {
            return [];
        }

        IEnumerable<ChestPreviewKind> ordered = chest.PreviewItems switch
        {
            1 or 2 => items.OrderBy(_ => Random.Shared.Next()),
            3 or 4 => Enumerable.Reverse(items),
            _ => items,
        };

        List<ChestPreviewKind> candidates = [.. ordered];

        if (chest.PreviewItems is 2 or 4 or 6)
        {
            candidates = [.. candidates.Distinct(), .. candidates];
        }

        // The dialog offers 1..4 and the client's visualization draws at most four icons; a chest
        // saved before the setting existed has a 0 it never chose.
        int amount = Math.Clamp(chest.PreviewAmount, 1, MaxPreviewItems);

        return [.. candidates.Take(amount)];
    }

    /// <summary>What the client's furni-chest visualization can draw at once.</summary>
    private const int MaxPreviewItems = 4;

    /// <summary>Loads the chest's own row, lets the caller change it, and saves. Every settings
    /// dialog is that same three-step, so it lives once: guard, load or create, apply, save.</summary>
    internal async Task UpdateChestAsync(
        ActionContext ctx,
        int chestId,
        Action<WiredChestEntity> apply,
        CancellationToken ct
    )
    {
        if (!await CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity chest = await WiredChestStore
                .GetOrOpenAsync(dbCtx, chestId, ct)
                .ConfigureAwait(true);

            apply(chest);

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            await ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to save settings for wired chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
            );
        }
    }

    /// <summary>The subset of the asked ids this player may actually put in a chest.</summary>
    internal async Task<List<int>> ReadDepositableIdsAsync(
        ActionContext ctx,
        ImmutableArray<int> itemIds,
        CancellationToken ct
    )
    {
        if (itemIds.IsDefaultOrEmpty)
        {
            return [];
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        List<FurnitureEntity> rows = await dbCtx
            .Furnitures.AsNoTracking()
            .Where(f =>
                itemIds.Contains(f.Id)
                && f.PlayerEntityId == (int)ctx.PlayerId
                && f.RoomEntityId == null
                && f.WiredChestEntityId == null
                && f.DeletedAt == null
            )
            .ToListAsync(ct)
            .ConfigureAwait(true);

        return
        [
            .. rows.Where(entity =>
                    _roomGrain
                        ._definitionProvider.TryGetDefinition(entity.FurnitureDefinitionEntityId)
                        ?.CanTrade == true
                )
                .Select(entity => entity.Id),
        ];
    }
}
