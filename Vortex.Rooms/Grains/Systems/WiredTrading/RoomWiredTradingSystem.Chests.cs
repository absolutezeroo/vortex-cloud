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
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Grains.Systems.WiredTrading;

/// <summary>
/// The chest screen: who has one open, what it shows, and the settings behind it.
/// </summary>
/// <remarks>
/// The lid is a room-wide appearance, so it stays open until the last viewer closes theirs, and
/// every mutation posts its delta to the other screens -- two people in one chest, one taking items
/// out, is the case a snapshot-at-open gets wrong.
/// </remarks>
public sealed partial class RoomWiredTradingSystem
{
    public async Task<WiredChestSnapshot?> OpenWiredChestAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || !_roomGrain._state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !WiredChestStore.IsChestLogic(item.Definition.LogicName)
        )
        {
            return null;
        }

        // Only whoever may decorate the room may look inside: a chest is stock, and its contents are
        // not public just because the furni is.
        RoomControllerType level = await _roomGrain
            .SecurityModule.GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity chest = await WiredChestStore
                .GetOrOpenAsync(dbCtx, chestId, ct)
                .ConfigureAwait(true);

            if (!_chestViewers.TryGetValue(chestId, out HashSet<PlayerId>? viewers))
            {
                viewers = [];
                _chestViewers[chestId] = viewers;
            }

            viewers.Add(ctx.PlayerId);

            // Also on open: a chest whose settings were saved before the furni carried them would
            // otherwise stay blank on screen forever. It is also what opens the lid and draws the
            // preview, for a chest set to open when someone looks inside.
            await _store.ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);

            return new WiredChestSnapshot
            {
                ChestId = chestId,
                Credits = chest.Credits,
                IsCoinChest = WiredChestStore.IsCoinChestLogic(item.Definition.LogicName),
            };
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to open wired chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
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
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await WiredChestStore
                .ReadAsync(dbCtx, chestId, ct)
                .ConfigureAwait(true);

            // No row means nobody ever opened it, so there is no lid to shut.
            if (chest is not null)
            {
                await _store
                    .ApplyChestSettingsToStuffDataAsync(chestId, chest)
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to close wired chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
            );
        }
    }

    public async Task<ImmutableArray<FurnitureItemSnapshot>?> ListWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    )
    {
        if (!await _store.CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await WiredChestStore
                .ReadAsync(dbCtx, chestId, ct)
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

            return [.. stored.Select(_store.ToChestItemSnapshot).OfType<FurnitureItemSnapshot>()];
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to list wired chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
            );

            return null;
        }
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
    /// Forgets a leaving player's trade screen and offer, and shuts the lids nobody is left
    /// looking into.
    /// </summary>
    /// <remarks>
    /// A player who walks out or disconnects sends no close, and without this they stay a viewer
    /// forever: the chest keeps its open appearance for the whole room, and every later delta is
    /// posted to a presence that is not there. The two neighbours of this call — the trade and the
    /// mystery boxes — clean up on the same event and for the same reason.
    /// <para>
    /// This is the one guaranteed exit, so it is the one that has to be complete. Deactivation is
    /// not guaranteed to be graceful and must never be the cleanup mechanism of record.
    /// </para>
    /// </remarks>
    internal async Task HandlePlayerLeftAsync(PlayerId playerId)
    {
        // An offer the player walked out on is an offer that failed, and the stack waiting on
        // wf_trg_transaction_failed has no other way to hear about a leaver.
        if (_sessions.Remove(playerId, out WiredTradeSession? abandoned) && abandoned.IsOffer)
        {
            await RaiseTransactionFailedAsync(playerId, CancellationToken.None)
                .ConfigureAwait(true);
        }

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
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(CancellationToken.None)
                .ConfigureAwait(true);

            foreach (int chestId in emptied)
            {
                WiredChestEntity? chest = await dbCtx
                    .WiredChests.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null)
                    .ConfigureAwait(true);

                if (chest is not null)
                {
                    await _store
                        .ApplyChestSettingsToStuffDataAsync(chestId, chest)
                        .ConfigureAwait(true);
                }
            }
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to shut the chests {ChestIds} a leaving player had open in room {RoomId}.",
                string.Join(", ", emptied),
                _roomGrain.RoomId
            );
        }
    }

    /// <summary>Whether anyone still has this chest's screen up.</summary>
    internal bool IsBeingLookedInto(int chestId) =>
        _chestViewers.TryGetValue(chestId, out HashSet<PlayerId>? viewers) && viewers.Count > 0;

    /// <summary>
    /// Sends a chest update to everyone looking at it except whoever caused it.
    /// </summary>
    /// <remarks>
    /// The delta message names no recipient — it is written to be broadcast — and the caller has
    /// already answered the player who asked. This is the other screens.
    /// </remarks>
    internal Task NotifyOtherChestViewersAsync(int chestId, PlayerId except, IComposer composer)
    {
        if (!_chestViewers.TryGetValue(chestId, out HashSet<PlayerId>? viewers))
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            viewers
                .Where(viewer => viewer != except)
                .Select(viewer =>
                    _roomGrain
                        ._grainFactory.GetPlayerPresenceGrain(viewer)
                        .SendComposerAsync(composer)
                )
        );
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
        _store.UpdateChestAsync(
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
        _store.UpdateChestAsync(
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
        _store.UpdateChestAsync(
            ctx,
            chestId,
            chest =>
            {
                chest.Locked = locked;
                chest.AutoLock = autoLock;
            },
            ct
        );

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

        RoomControllerType level = await _roomGrain
            .SecurityModule.GetControllerLevelAsync(ctx)
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
            .. _roomGrain
                ._state.ItemsById.Values.Where(item =>
                    WiredChestStore.IsChestLogic(item.Definition.LogicName)
                )
                .Select(item => item.ObjectId.Value),
        ];

        if (chestIds.Count == 0)
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
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
                await _store
                    .ApplyChestSettingsToStuffDataAsync(chest.FurnitureEntityId, chest)
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to lock the chests of room {RoomId}.",
                _roomGrain.RoomId
            );
        }
    }
}
