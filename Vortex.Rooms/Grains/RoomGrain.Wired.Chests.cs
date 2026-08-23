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
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Wired chests: the furni that hold real value for a room's wiring to hand out.
/// </summary>
public sealed partial class RoomGrain
{
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

    private static bool IsCoinChestLogic(string logicName) =>
        string.Equals(logicName, CoinChestLogic, StringComparison.Ordinal);

    internal static bool IsChestLogic(string logicName) =>
        string.Equals(logicName, CoinChestLogic, StringComparison.Ordinal)
        || string.Equals(logicName, FurniChestLogic, StringComparison.Ordinal);

    public async Task<WiredChestSnapshot?> WithdrawWiredChestCreditsAsync(
        ActionContext ctx,
        int chestId,
        int amount,
        CancellationToken ct
    )
    {
        if (!await CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            // A locked chest refuses withdrawals — the wired pay-out already reads it this way, and
            // a lock the owner's own withdraw button walks straight through is not a lock.
            if (chest is null || chest.Credits <= 0 || chest.Locked)
            {
                return null;
            }

            // The button for "everything" asks for it by sending nothing sensible, and a request
            // for more than the chest holds is not an error, it is a request for the rest.
            int taken = amount <= 0 ? chest.Credits : Math.Min(amount, chest.Credits);

            chest.Credits -= taken;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            // The chest is emptied first and put back if the wallet refuses, the same way a
            // purchase refunds itself: money that left a chest and reached nobody is money lost,
            // and a currency with no currency_types row refuses silently unless this is read.
            bool landed = await _grainFactory
                .GetPlayerWalletGrain(ctx.PlayerId)
                .GrantCurrencyAsync(
                    new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    taken,
                    ct
                )
                .ConfigureAwait(true);

            if (!landed)
            {
                chest.Credits += taken;

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

                _logger.LogWarning(
                    "Wired chest {ChestId} in room {RoomId} could not pay {Amount} credits to player {PlayerId}; the chest keeps them.",
                    chestId,
                    RoomId,
                    taken,
                    ctx.PlayerId
                );

                return null;
            }

            // Logged only here, past the wallet: a movement that was rolled back never happened,
            // and a log that says otherwise is worse than no log.
            dbCtx.WiredChestTransactions.Add(
                NewManualTransaction(ctx.PlayerId, chest.Id, withdrawCoins: taken)
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            return await SnapshotAsync(chestId, chest.Credits).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to withdraw from wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>Whether this player may operate that chest: it has to be a chest, standing in this
    /// room, and they have to be allowed to decorate.</summary>
    private async Task<bool> CanUseChestAsync(ActionContext ctx, int chestId)
    {
        if (
            ctx.PlayerId <= 0
            || !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !IsChestLogic(item.Definition.LogicName)
        )
        {
            return false;
        }

        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        return level != RoomControllerType.None;
    }

    private Task<WiredChestSnapshot?> SnapshotAsync(int chestId, int credits) =>
        Task.FromResult<WiredChestSnapshot?>(
            _state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
                ? new WiredChestSnapshot
                {
                    ChestId = chestId,
                    Credits = credits,
                    IsCoinChest = IsCoinChestLogic(item.Definition.LogicName),
                }
                : null
        );

    public async Task<WiredChestSnapshot?> OpenWiredChestAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !IsChestLogic(item.Definition.LogicName)
        )
        {
            return null;
        }

        // Only whoever may decorate the room may look inside: a chest is stock, and its contents are
        // not public just because the furni is.
        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (chest is null)
            {
                chest = new WiredChestEntity
                {
                    FurnitureEntityId = chestId,
                    Credits = 0,
                    NotificationsEnabled = true,
                };

                dbCtx.WiredChests.Add(chest);

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }

            if (!_chestViewers.TryGetValue(chestId, out HashSet<PlayerId>? viewers))
            {
                viewers = [];
                _chestViewers[chestId] = viewers;
            }

            viewers.Add(ctx.PlayerId);

            // Also on open: a chest whose settings were saved before the furni carried them would
            // otherwise stay blank on screen forever. It is also what opens the lid and draws the
            // preview, for a chest set to open when someone looks inside.
            await ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);

            return new WiredChestSnapshot
            {
                ChestId = chestId,
                Credits = chest.Credits,
                IsCoinChest = IsCoinChestLogic(item.Definition.LogicName),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to open wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return null;
        }
    }

    public async Task CloseWiredChestAsync(ActionContext ctx, int chestId, CancellationToken ct)
    {
        if (
            !_chestViewers.TryGetValue(chestId, out HashSet<PlayerId>? viewers)
            || !viewers.Remove(ctx.PlayerId)
        )
        {
            return;
        }

        // Someone else is still looking: nothing to shut, and re-applying would only rewrite the
        // same state.
        if (viewers.Count > 0)
        {
            return;
        }

        _chestViewers.Remove(chestId);

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.AsNoTracking()
                .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null, ct)
                .ConfigureAwait(true);

            // No row means nobody ever opened it, so there is no lid to shut.
            if (chest is not null)
            {
                await ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to close wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );
        }
    }

    /// <summary>Turns a stored row into the item shape the chest screen and the inventory both
    /// speak. Owner name is left empty on purpose: the chest screen never shows it, and resolving a
    /// name per row would cost a lookup per item for nothing.</summary>
    private FurnitureItemSnapshot? ToChestItemSnapshot(FurnitureEntity entity)
    {
        FurnitureDefinitionSnapshot? definition = _definitionProvider.TryGetDefinition(
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
            StuffData = _stuffDataFactory
                .CreateStuffDataFromJson(definition.StuffDataType, entity.ExtraData)
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
    private static bool IsSameKind(
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

    public async Task<ImmutableArray<FurnitureItemSnapshot>?> ListWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    )
    {
        if (!await CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.AsNoTracking()
                .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null, ct)
                .ConfigureAwait(true);

            // A chest nobody has opened yet has no row and holds nothing. That is an empty chest,
            // not a failure: the screen still has to open.
            if (chest is null)
            {
                return ImmutableArray<FurnitureItemSnapshot>.Empty;
            }

            List<FurnitureEntity> stored = await dbCtx
                .Furnitures.AsNoTracking()
                .Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return [.. stored.Select(ToChestItemSnapshot).OfType<FurnitureItemSnapshot>()];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to list wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return null;
        }
    }

    public async Task<ImmutableArray<int>> WithdrawWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        bool isWallItem,
        int typeId,
        string legacyPosterId,
        int count,
        CancellationToken ct
    )
    {
        if (count <= 0 || !await CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return [];
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.AsNoTracking()
                .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null, ct)
                .ConfigureAwait(true);

            // A locked chest refuses withdrawals, furniture included: the lock guards what leaves,
            // and taking the stock out by hand is the plainest way for it to leave.
            if (chest is null || chest.Locked)
            {
                return [];
            }

            List<FurnitureEntity> stored = await dbCtx
                .Furnitures.Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            // The kind is decided on the snapshot rather than on the row: sprite, wall/floor and the
            // poster number all live on the definition or in the stuff data, not in columns.
            List<FurnitureEntity> leaving =
            [
                .. stored
                    .Where(entity =>
                    {
                        FurnitureItemSnapshot? snapshot = ToChestItemSnapshot(entity);

                        return snapshot is not null
                            && IsSameKind(snapshot, isWallItem, typeId, legacyPosterId);
                    })
                    .Take(count),
            ];

            if (leaving.Count == 0)
            {
                return [];
            }

            foreach (FurnitureEntity entity in leaving)
            {
                entity.WiredChestEntityId = null;
                entity.RoomEntityId = null;
                entity.PlayerEntityId = (int)ctx.PlayerId;
            }

            dbCtx.WiredChestTransactions.Add(
                NewManualTransaction(
                    ctx.PlayerId,
                    chest.Id,
                    withdrawFurni: leaving.Count,
                    definitionInfo: string.Join(
                        ", ",
                        leaving
                            .Select(entity =>
                                _definitionProvider
                                    .TryGetDefinition(entity.FurnitureDefinitionEntityId)
                                    ?.Name
                                ?? string.Empty
                            )
                            .Distinct()
                    )
                )
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            // The row is the player's now; the inventory grain still has to be told, because its
            // list is a cache built at activation and nothing reloads it on its own.
            await _grainFactory
                .GetInventoryGrain(ctx.PlayerId)
                .ReloadFurnitureAsync(ct)
                .ConfigureAwait(true);

            // The preview is drawn from what the chest holds, so it is now stale.
            await ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);

            ImmutableArray<int> removed = [.. leaving.Select(entity => entity.Id)];

            // The caller answers the player who asked; these are the other windows open on the
            // same chest, which would otherwise keep showing rows that have left.
            await NotifyOtherChestViewersAsync(
                    chestId,
                    ctx.PlayerId,
                    new WiredChestItemsUpdateMessageComposer
                    {
                        ChestId = chestId,
                        RemovedItemIds = removed,
                        AddedItems = ImmutableArray<FurnitureItemSnapshot>.Empty,
                    }
                )
                .ConfigureAwait(true);

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to withdraw items from wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return [];
        }
    }

    /// <summary>
    /// Copies a chest's saved settings onto the furni the client reads them from.
    /// </summary>
    /// <remarks>
    /// The dialogs prefill from the furni's map stuff data, never from a message, so a chest whose
    /// settings live only in the table shows a blank screen. The keys themselves are
    /// <see cref="WiredChestStuffData" />'s business, not this grain's.
    /// </remarks>
    private async Task ApplyChestSettingsToStuffDataAsync(int chestId, WiredChestEntity chest)
    {
        if (
            !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || item.Logic.StuffData is not IMapStuffData map
        )
        {
            return;
        }

        WiredChestStuffData.Apply(map, chest);

        bool? open = ResolveChestOpenState(chest, IsBeingLookedInto(chestId));

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

    /// <summary>Who currently has each chest open on screen.</summary>
    /// <remarks>
    /// Two things read it, and both need the viewers rather than a flag: the lid stays open until
    /// the *last* one leaves, and a withdrawal or a deposit has to reach the screens other people
    /// are looking at. Without that second half, two players in the same room saw different
    /// contents — one took items out and the other's window kept showing them.
    /// </remarks>
    private readonly Dictionary<int, HashSet<PlayerId>> _chestViewers = [];

    /// <summary>
    /// Forgets a leaving player's chest screens, and shuts the lids nobody is left looking into.
    /// </summary>
    /// <remarks>
    /// A player who walks out or disconnects sends no close, and without this they stay a viewer
    /// forever: the chest keeps its open appearance for the whole room, and every later delta is
    /// posted to a presence that is not there. The two neighbours of this call — the trade and the
    /// mystery boxes — clean up on the same event and for the same reason.
    /// </remarks>
    private async Task CloseChestScreensForLeavingPlayerAsync(PlayerId playerId)
    {
        _chestDeposits.Remove(playerId);

        // An offer the player walked out on is an offer that failed, and the stack waiting on
        // wf_trg_transaction_failed has no other way to hear about a leaver. Forgetting only the
        // screen would leave the offer itself pending for as long as the room lives.
        await CancelTransactionAsync(0, playerId, CancellationToken.None).ConfigureAwait(true);

        List<int> emptied = [];

        foreach ((int chestId, HashSet<PlayerId> viewers) in _chestViewers)
        {
            if (viewers.Remove(playerId) && viewers.Count == 0)
            {
                emptied.Add(chestId);
            }
        }

        foreach (int chestId in emptied)
        {
            _chestViewers.Remove(chestId);
        }

        if (emptied.Count == 0)
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(CancellationToken.None)
                .ConfigureAwait(true);

            foreach (int chestId in emptied)
            {
                WiredChestEntity? chest = await dbCtx
                    .WiredChests.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null)
                    .ConfigureAwait(true);

                if (chest is not null)
                {
                    await ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to shut the chests {ChestIds} a leaving player had open in room {RoomId}.",
                string.Join(", ", emptied),
                RoomId
            );
        }
    }

    /// <summary>Whether anyone still has this chest's screen up.</summary>
    private bool IsBeingLookedInto(int chestId) =>
        _chestViewers.TryGetValue(chestId, out HashSet<PlayerId>? viewers) && viewers.Count > 0;

    /// <summary>
    /// Sends a chest update to everyone looking at it except whoever caused it.
    /// </summary>
    /// <remarks>
    /// The delta message names no recipient — it is written to be broadcast — and the caller has
    /// already answered the player who asked. This is the other screens.
    /// </remarks>
    private Task NotifyOtherChestViewersAsync(int chestId, PlayerId except, IComposer composer)
    {
        if (!_chestViewers.TryGetValue(chestId, out HashSet<PlayerId>? viewers))
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            viewers
                .Where(viewer => viewer != except)
                .Select(viewer =>
                    _grainFactory.GetPlayerPresenceGrain(viewer).SendComposerAsync(composer)
                )
        );
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

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(CancellationToken.None)
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
    private async Task UpdateChestAsync(
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
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            // Settings can be saved on a chest nobody has opened yet, so the row is created here too
            // rather than only on open.
            if (chest is null)
            {
                chest = new WiredChestEntity
                {
                    FurnitureEntityId = chestId,
                    Credits = 0,
                    NotificationsEnabled = true,
                };

                dbCtx.WiredChests.Add(chest);
            }

            apply(chest);

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            await ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to save settings for wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );
        }
    }

    public Task SaveWiredChestSettingsAsync(
        ActionContext ctx,
        int chestId,
        string name,
        string description,
        bool everyoneCanOpen,
        bool everyoneCanDonate,
        int chestState,
        int previewItems,
        int previewAmount,
        CancellationToken ct
    ) =>
        UpdateChestAsync(
            ctx,
            chestId,
            chest =>
            {
                chest.Name = name;
                chest.Description = description;
                chest.EveryoneCanOpen = everyoneCanOpen;
                chest.EveryoneCanDonate = everyoneCanDonate;
                chest.ChestState = chestState;
                chest.PreviewItems = previewItems;
                chest.PreviewAmount = previewAmount;
            },
            ct
        );

    public Task SaveWiredChestNotificationSettingsAsync(
        ActionContext ctx,
        int chestId,
        int notificationMode,
        bool notifyWhenFull,
        bool notifyOnDonation,
        bool notifyOnWithdraw,
        bool notifyWhenEmpty,
        bool notifyOnAnyWiredTransaction,
        CancellationToken ct
    ) =>
        UpdateChestAsync(
            ctx,
            chestId,
            chest =>
            {
                chest.NotificationMode = notificationMode;
                chest.NotifyWhenFull = notifyWhenFull;
                chest.NotifyOnDonation = notifyOnDonation;
                chest.NotifyOnWithdraw = notifyOnWithdraw;
                chest.NotifyWhenEmpty = notifyWhenEmpty;
                chest.NotifyOnAnyWiredTransaction = notifyOnAnyWiredTransaction;

                // The one flag that predates the dialog: keep it in step with what the dialog says,
                // so nothing reads a chest as silent while its checkboxes say otherwise.
                chest.NotificationsEnabled =
                    notifyWhenFull
                    || notifyOnDonation
                    || notifyOnWithdraw
                    || notifyWhenEmpty
                    || notifyOnAnyWiredTransaction;
            },
            ct
        );

    public Task SetWiredChestLockAsync(
        ActionContext ctx,
        int chestId,
        bool locked,
        bool autoLock,
        CancellationToken ct
    ) =>
        UpdateChestAsync(
            ctx,
            chestId,
            chest =>
            {
                chest.Locked = locked;
                chest.AutoLock = autoLock;
            },
            ct
        );

    /// <summary>A row for something a person did by hand. Type 0 is the client's MANUAL; the wired,
    /// contract and auto-withdraw types need machinery this hotel does not have yet.</summary>
    private WiredChestTransactionEntity NewManualTransaction(
        PlayerId playerId,
        int chestRowId,
        int withdrawCoins = 0,
        int withdrawFurni = 0,
        int depositFurni = 0,
        string definitionInfo = ""
    ) =>
        new()
        {
            WiredChestEntityId = chestRowId,
            RoomEntityId = (int)RoomId,
            TransactionType = ManualTransaction,
            DefinitionInfo = definitionInfo,
            PlayerEntityId = (int)playerId,
            PlayerName = ResolvePlayerName(playerId),
            // One row, one chest — the entity says as much, and nothing a player does by hand
            // touches two. The zero below is a fact, not a placeholder: nothing a player does by
            // hand puts coins into a chest, because the client has no way to name an amount. A
            // contract does, and writes its own row — the amount there is the contract's, not
            // something anyone typed.
            ChestCount = 1,
            WithdrawFurniCount = withdrawFurni,
            DepositFurniCount = depositFurni,
            WithdrawCoinsCount = withdrawCoins,
            DepositCoinsCount = 0,
        };

    /// <summary>What the client calls MANUAL.</summary>
    private const int ManualTransaction = 0;

    /// <summary>The two log lists the client knows: a chest, or the room the chests stand in. It
    /// reads the type back off the page to decide what it is looking at.</summary>
    private const int ChestLogList = 0;
    private const int RoomLogList = 1;

    /// <summary>The page size comes straight from the client, so it is clamped. Asking for a
    /// million rows should cost a page, not the table.</summary>
    private const int MaxLogPageSize = 200;

    /// <summary>The player's name as it is right now, for a row that will outlive it. Taken from the
    /// room rather than the database: whoever moves something is standing in it.</summary>
    private string ResolvePlayerName(PlayerId playerId) =>
        _state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
        && _state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            ? avatar.Name
            : string.Empty;

    private static WiredTransactionSnapshot ToTransactionSnapshot(
        WiredChestTransactionEntity row
    ) =>
        new()
        {
            TransactionId = row.Id,
            RoomId = row.RoomEntityId,
            TransactionType = row.TransactionType,
            DefinitionInfo = row.DefinitionInfo,
            PlayerId = row.PlayerEntityId,
            PlayerName = row.PlayerName,
            Timestamp = new DateTimeOffset(row.CreatedAt, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            ReadableTimestamp = row.CreatedAt.ToString(
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture
            ),
            ChestCount = row.ChestCount,
            WithdrawFurniCount = row.WithdrawFurniCount,
            DepositFurniCount = row.DepositFurniCount,
            WithdrawCoinsCount = row.WithdrawCoinsCount,
            DepositCoinsCount = row.DepositCoinsCount,
        };

    private async Task<WiredTransactionsSnapshot?> ReadTransactionsAsync(
        int logListType,
        long logListId,
        int? chestRowId,
        int pageSize,
        int page,
        CancellationToken ct
    )
    {
        int size = Math.Clamp(pageSize, 1, MaxLogPageSize);
        int wanted = Math.Max(1, page);

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            IQueryable<WiredChestTransactionEntity> rows = dbCtx
                .WiredChestTransactions.AsNoTracking()
                .Where(t => t.DeletedAt == null);

            rows = chestRowId is null
                ? rows.Where(t => t.RoomEntityId == (int)RoomId)
                : rows.Where(t => t.WiredChestEntityId == chestRowId);

            int total = await rows.CountAsync(ct).ConfigureAwait(true);

            List<WiredChestTransactionEntity> paged = await rows.OrderByDescending(t => t.Id)
                .Skip((wanted - 1) * size)
                .Take(size)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return new WiredTransactionsSnapshot
            {
                LogListType = logListType,
                LogListId = logListId,
                TotalLogs = total,
                CurrentPage = wanted,
                Amount = size,
                Logs = [.. paged.Select(ToTransactionSnapshot)],
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read chest transactions in room {RoomId}.", RoomId);

            return null;
        }
    }

    public async Task<WiredTransactionsSnapshot?> GetWiredChestTransactionsAsync(
        ActionContext ctx,
        int chestId,
        int pageSize,
        int page,
        CancellationToken ct
    )
    {
        if (!await CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return null;
        }

        int? chestRowId = await ResolveChestRowIdAsync(chestId, ct).ConfigureAwait(true);

        // A chest nobody has touched has no row and therefore no history. That is an empty page,
        // not a refusal: the screen still opens.
        return await ReadTransactionsAsync(
                ChestLogList,
                chestId,
                chestRowId ?? -1,
                pageSize,
                page,
                ct
            )
            .ConfigureAwait(true);
    }

    /// <summary>
    /// One row of the log, opened.
    /// </summary>
    /// <remarks>
    /// The breakdown is rebuilt from the row's own <c>DefinitionInfo</c> — the names of what moved,
    /// in order, repeated once per item — because that is what a row stores. Names resolve back to
    /// definitions, and two of the same name are one line with a count, which is exactly what the
    /// window renders. A name that no longer resolves is dropped and makes the answer incomplete,
    /// so the window says "and more" rather than quietly under-reporting.
    /// <para>
    /// Withdrawals and deposits are told apart by the row's own counters, not by the text: one row
    /// is one direction, and a row that moved nothing has an empty breakdown either way.
    /// </para>
    /// </remarks>
    public async Task<WiredTransactionDetailsSnapshot?> GetWiredTransactionDetailsAsync(
        ActionContext ctx,
        long transactionId,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || transactionId <= 0)
        {
            return null;
        }

        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestTransactionEntity? row = await dbCtx
                .WiredChestTransactions.AsNoTracking()
                .FirstOrDefaultAsync(
                    t =>
                        t.Id == transactionId
                        && t.RoomEntityId == (int)RoomId
                        && t.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (row is null)
            {
                return null;
            }

            int chestFurniId = await dbCtx
                .WiredChests.AsNoTracking()
                .Where(chest => chest.Id == row.WiredChestEntityId)
                .Select(chest => chest.FurnitureEntityId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(true);

            List<WiredTransactionItemCount> moved = BuildItemBreakdown(
                row.DefinitionInfo,
                out bool incomplete
            );

            bool isWithdrawal = row.WithdrawFurniCount > 0;

            return new WiredTransactionDetailsSnapshot
            {
                Info = ToTransactionSnapshot(row),
                ChestIds = chestFurniId > 0 ? [chestFurniId] : [],
                Deposited = isWithdrawal ? [] : [.. moved],
                Withdrawn = isWithdrawal ? [.. moved] : [],
                IsIncompleteData = incomplete,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to read transaction {TransactionId} in room {RoomId}.",
                transactionId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>
    /// The names a row recorded, counted by kind.
    /// </summary>
    /// <remarks>
    /// A row stores what moved as names joined by ", ", so a name containing that separator would
    /// split wrongly — none does, and furniture names are database identifiers rather than free
    /// text. <paramref name="incomplete" /> is set when a name resolves to nothing, which is the
    /// only way this can lose an item.
    /// </remarks>
    private List<WiredTransactionItemCount> BuildItemBreakdown(
        string definitionInfo,
        out bool incomplete
    )
    {
        incomplete = false;

        List<WiredTransactionItemCount> items = [];

        if (string.IsNullOrWhiteSpace(definitionInfo))
        {
            return items;
        }

        Dictionary<int, WiredTransactionItemCount> byDefinition = [];

        foreach (string name in definitionInfo.Split(',', StringSplitOptions.TrimEntries))
        {
            FurnitureDefinitionSnapshot? definition = _definitionProvider.TryGetDefinitionByName(
                name
            );

            if (definition is null)
            {
                incomplete = true;

                continue;
            }

            byDefinition[definition.Id] = byDefinition.TryGetValue(
                definition.Id,
                out WiredTransactionItemCount? seen
            )
                ? seen with
                {
                    Count = seen.Count + 1,
                }
                : new WiredTransactionItemCount
                {
                    IsWallItem = definition.ProductType == ProductType.Wall,
                    SpriteId = definition.SpriteId,
                    LegacyPosterId = string.Empty,
                    Count = 1,
                };
        }

        items.AddRange(byDefinition.Values);

        return items;
    }

    public async Task<WiredTransactionsSnapshot?> GetWiredRoomTransactionsAsync(
        ActionContext ctx,
        int pageSize,
        int page,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return null;
        }

        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return null;
        }

        return await ReadTransactionsAsync(RoomLogList, (int)RoomId, null, pageSize, page, ct)
            .ConfigureAwait(true);
    }

    private async Task<int?> ResolveChestRowIdAsync(int chestId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        WiredChestEntity? chest = await dbCtx
            .WiredChests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null, ct)
            .ConfigureAwait(true);

        return chest?.Id;
    }

    public async Task SetAllWiredChestLocksAsync(
        ActionContext ctx,
        bool locked,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return;
        }

        // The room's chests are the ones standing in it, which is what this grain knows; the table
        // holds rows for chests that have been picked up since, and those are not this room's to
        // lock.
        List<int> chestIds =
        [
            .. _state
                .ItemsById.Values.Where(item => IsChestLogic(item.Definition.LogicName))
                .Select(item => item.ObjectId.Value),
        ];

        if (chestIds.Count == 0)
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<WiredChestEntity> chests = await dbCtx
                .WiredChests.Where(c =>
                    chestIds.Contains(c.FurnitureEntityId) && c.DeletedAt == null
                )
                .ToListAsync(ct)
                .ConfigureAwait(true);

            foreach (WiredChestEntity chest in chests)
            {
                chest.Locked = locked;
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            foreach (WiredChestEntity chest in chests)
            {
                await ApplyChestSettingsToStuffDataAsync(chest.FurnitureEntityId, chest)
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to lock the chests of room {RoomId}.", RoomId);
        }
    }

    /// <summary>What the client calls WIRED: a box moved this, not a person.</summary>
    private const int WiredTransaction = 1;

    private WiredChestTransactionEntity NewWiredTransaction(
        PlayerId playerId,
        int chestRowId,
        int withdrawCoins = 0,
        int withdrawFurni = 0,
        string definitionInfo = ""
    ) =>
        new()
        {
            WiredChestEntityId = chestRowId,
            RoomEntityId = (int)RoomId,
            TransactionType = WiredTransaction,
            DefinitionInfo = definitionInfo,
            PlayerEntityId = (int)playerId,
            PlayerName = ResolvePlayerName(playerId),
            // Same one-row-one-chest reading as the manual builder. Both deposit counters are zero
            // by what this path is: wiring pays *out* of a chest, it never fills one.
            ChestCount = 1,
            WithdrawFurniCount = withdrawFurni,
            DepositFurniCount = 0,
            WithdrawCoinsCount = withdrawCoins,
            DepositCoinsCount = 0,
        };

    public async Task<int> PayOutWiredChestCreditsAsync(
        int chestId,
        PlayerId playerId,
        int amount,
        bool everything,
        CancellationToken ct
    )
    {
        if (
            playerId <= 0
            || (!everything && amount <= 0)
            || !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !IsCoinChestLogic(item.Definition.LogicName)
        )
        {
            return 0;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (chest is null || chest.Credits <= 0 || chest.Locked)
            {
                return 0;
            }

            int taken = everything ? chest.Credits : Math.Min(amount, chest.Credits);

            chest.Credits -= taken;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            bool landed = await _grainFactory
                .GetPlayerWalletGrain(playerId)
                .GrantCurrencyAsync(
                    new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    taken,
                    ct
                )
                .ConfigureAwait(true);

            if (!landed)
            {
                chest.Credits += taken;

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

                return 0;
            }

            dbCtx.WiredChestTransactions.Add(
                NewWiredTransaction(playerId, chest.Id, withdrawCoins: taken)
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            await ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);

            return taken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Wired pay-out failed for chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return 0;
        }
    }

    public async Task<int> PayOutWiredChestItemsAsync(
        int chestId,
        PlayerId playerId,
        int count,
        CancellationToken ct
    )
    {
        if (
            playerId <= 0
            || count <= 0
            || !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !IsChestLogic(item.Definition.LogicName)
            || IsCoinChestLogic(item.Definition.LogicName)
        )
        {
            return 0;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (chest is null || chest.Locked)
            {
                return 0;
            }

            List<FurnitureEntity> leaving = await dbCtx
                .Furnitures.Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
                .Take(count)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            if (leaving.Count == 0)
            {
                return 0;
            }

            foreach (FurnitureEntity entity in leaving)
            {
                entity.WiredChestEntityId = null;
                entity.RoomEntityId = null;
                entity.PlayerEntityId = (int)playerId;
            }

            dbCtx.WiredChestTransactions.Add(
                NewWiredTransaction(playerId, chest.Id, withdrawFurni: leaving.Count)
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            await _grainFactory
                .GetInventoryGrain(playerId)
                .ReloadFurnitureAsync(ct)
                .ConfigureAwait(true);

            return leaving.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Wired furni pay-out failed for chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return 0;
        }
    }
}
