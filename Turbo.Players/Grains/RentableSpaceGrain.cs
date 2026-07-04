using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Turbo.Database.Context;
using Turbo.Database.Entities.Furniture;
using Turbo.Primitives.Action;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Outgoing.Room.Furniture;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Wallet;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Grains;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Snapshots;

namespace Turbo.Players.Grains;

/// <summary>
///     Per-instance grain (key = furniture id) for one placed rentable-space item.
///     Serializes all rent / cancel / expire transitions; all DB mutations go through here.
/// </summary>
internal sealed class RentableSpaceGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<RentableSpaceGrain> logger
) : Grain, IRentableSpaceGrain
{
    private IGrainTimer? _expiryTimer;
    private string _renterName = string.Empty;

    // Hot state — kept in sync with DB after every mutation.
    private RoomRentableSpaceEntity? _space;

    // Cold config (loaded once, never mutated at runtime).
    private RentableSpaceTermsEntity? _terms;
    private RoomId? _roomId;
    private int FurnitureId => (int)this.GetPrimaryKeyLong();

    public Task<RentableSpaceStatusSnapshot> GetStatusAsync(
        int viewerPlayerId,
        CancellationToken ct
    )
    {
        DateTime now = DateTime.UtcNow;
        bool rented =
            _space?.RenterPlayerEntityId is not null
            && _space.RentedUntil is not null
            && _space.RentedUntil.Value > now;

        int remaining = rented ? (int)(_space!.RentedUntil!.Value - now).TotalSeconds : 0;

        RentableSpaceRentFailedType errorCode =
            rented ? RentableSpaceRentFailedType.AlreadyRented
            : _terms is null ? RentableSpaceRentFailedType.Disabled
            : RentableSpaceRentFailedType.None;

        return Task.FromResult(
            new RentableSpaceStatusSnapshot
            {
                Rented = rented,
                CanRentErrorCode = errorCode,
                RenterId = _space?.RenterPlayerEntityId ?? 0,
                RenterName = _renterName,
                TimeRemaining = remaining,
                Price = _terms?.Price ?? 0,
                CurrencyName = _terms?.CurrencyTypeEntity?.Name ?? string.Empty,
            }
        );
    }

    public async Task<int?> RentAsync(int renterPlayerId, CancellationToken ct)
    {
        if (_terms is null)
        {
            return (int)RentableSpaceRentFailedType.Disabled;
        }

        DateTime now = DateTime.UtcNow;

        // Already rented by someone.
        if (
            _space?.RenterPlayerEntityId is not null
            && _space.RentedUntil is not null
            && _space.RentedUntil.Value > now
        )
        {
            return (int)RentableSpaceRentFailedType.AlreadyRented;
        }

        await using TurboDbContext db = await dbCtxFactory.CreateDbContextAsync(ct);

        // HC check.
        if (_terms.RequiresHc)
        {
            bool hasHc = await db
                .PlayerSubscriptions.AsNoTracking()
                .AnyAsync(
                    s =>
                        s.PlayerEntityId == renterPlayerId
                        && s.SubscriptionType == SubscriptionType.HabboClub
                        && s.DeletedAt == null
                        && s.ExpiresAt > now,
                    ct
                );

            if (!hasHc)
            {
                return (int)RentableSpaceRentFailedType.NoHabboClub;
            }
        }

        // One space per player (DATA-MODEL §3.4 — via renter_player_id index).
        bool alreadyRenting = await db
            .RoomRentableSpaces.AsNoTracking()
            .AnyAsync(
                s =>
                    s.RenterPlayerEntityId == renterPlayerId
                    && s.DeletedAt == null
                    && s.RentedUntil > now,
                ct
            );

        if (alreadyRenting)
        {
            return (int)RentableSpaceRentFailedType.CanRentOnlyOneSpace;
        }

        // Debit wallet (also writes the ledger entry via CurrencyChangedEvent).
        CurrencyKind kind = new()
        {
            CurrencyType = _terms.CurrencyTypeEntity.CurrencyType,
            ActivityPointType = _terms.CurrencyTypeEntity.ActivityPointType,
        };

        WalletDebitResult debitResult = await grainFactory
            .GetPlayerWalletGrain((long)renterPlayerId)
            .TryDebitAsync(
                [new WalletDebitRequest { CurrencyKind = kind, Amount = _terms.Price }],
                ct
            );

        if (!debitResult.Succeeded)
        {
            return _terms.CurrencyTypeEntity.CurrencyType == CurrencyType.Credits
                ? (int)RentableSpaceRentFailedType.NotEnoughCredits
                : (int)RentableSpaceRentFailedType.NotEnoughDuckets;
        }

        DateTime rentedUntil = now.AddSeconds(_terms.RentDurationSeconds);

        // Load furniture entity (needed for both upsert navigation and owner credit).
        FurnitureEntity? furniEntity = await db.Furnitures.FirstOrDefaultAsync(
            f => f.Id == FurnitureId,
            ct
        );

        if (furniEntity is null)
        {
            return (int)RentableSpaceRentFailedType.Generic;
        }

        // Upsert state row (insert first time, update in-place thereafter).
        if (_space is null)
        {
            RoomRentableSpaceEntity newSpace = new()
            {
                FurnitureEntityId = FurnitureId,
                RenterPlayerEntityId = renterPlayerId,
                RentedUntil = rentedUntil,
                FurnitureEntity = furniEntity,
            };
            db.RoomRentableSpaces.Add(newSpace);
            await db.SaveChangesAsync(ct);
            _space = newSpace;
        }
        else
        {
            RoomRentableSpaceEntity? entity = await db.RoomRentableSpaces.FirstOrDefaultAsync(
                s => s.FurnitureEntityId == FurnitureId && s.DeletedAt == null,
                ct
            );

            if (entity is not null)
            {
                entity.RenterPlayerEntityId = renterPlayerId;
                entity.RentedUntil = rentedUntil;
                await db.SaveChangesAsync(ct);
                _space.RenterPlayerEntityId = renterPlayerId;
                _space.RentedUntil = rentedUntil;
            }
        }

        // Credit the furniture owner (skip if renter == owner to avoid self-transfer).
        if (furniEntity.PlayerEntityId != renterPlayerId)
        {
            IPlayerWalletGrain ownerWallet = grainFactory.GetPlayerWalletGrain(
                (long)furniEntity.PlayerEntityId
            );

            if (kind.CurrencyType == CurrencyType.Credits)
            {
                await ownerWallet.GrantCreditsAsync(_terms.Price, ct).ConfigureAwait(false);
            }
            else if (kind.ActivityPointType.HasValue)
            {
                await ownerWallet
                    .GrantActivityPointsAsync(kind.ActivityPointType.Value, _terms.Price, ct)
                    .ConfigureAwait(false);
            }
        }

        string renterName =
            await db
                .Players.Where(p => p.Id == renterPlayerId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct)
            ?? string.Empty;

        _renterName = renterName;
        ScheduleExpiryTimer(rentedUntil);
        await SetVisualStateAsync(3, ct);

        await events
            .PublishAsync(
                new RentalStartedEvent(
                    FurnitureId,
                    renterPlayerId,
                    _roomId ?? 0,
                    _terms.Price,
                    _terms.CurrencyTypeEntity.CurrencyType.ToString(),
                    new DateTimeOffset(rentedUntil, TimeSpan.Zero)
                ),
                ct
            )
            .ConfigureAwait(false);

        return null; // success
    }

    public async Task<bool> CancelRentAsync(int actorPlayerId, bool isStaff, CancellationToken ct)
    {
        DateTime now = DateTime.UtcNow;
        if (
            _space?.RenterPlayerEntityId is null
            || _space.RentedUntil is null
            || _space.RentedUntil.Value <= now
        )
        {
            return false;
        }

        // Verify actor: furni owner OR staff.
        if (!isStaff)
        {
            await using TurboDbContext authDb = await dbCtxFactory.CreateDbContextAsync(ct);
            int? furniOwnerId = await authDb
                .Furnitures.Where(f => f.Id == FurnitureId)
                .Select(f => (int?)f.PlayerEntityId)
                .FirstOrDefaultAsync(ct);

            if (furniOwnerId != actorPlayerId)
            {
                return false;
            }
        }

        int renterId = _space.RenterPlayerEntityId.Value;
        await ClearRentalAsync(renterId, ct);
        return true;
    }

    public async Task ExpireAsync(CancellationToken ct)
    {
        DateTime now = DateTime.UtcNow;
        if (
            _space?.RenterPlayerEntityId is null
            || _space.RentedUntil is null
            || _space.RentedUntil.Value > now
        )
        {
            return;
        }

        int renterId = _space.RenterPlayerEntityId.Value;
        await ClearRentalAsync(renterId, ct);

        await events
            .PublishAsync(new RentalExpiredEvent(FurnitureId, renterId, _roomId ?? 0), ct)
            .ConfigureAwait(false);
    }

    public async Task<RentableSpaceConfigSnapshot> GetConfigAsync(CancellationToken ct)
    {
        await using TurboDbContext db = await dbCtxFactory.CreateDbContextAsync(ct);

        List<AvailableCurrencySnapshot> currencies = await db
            .CurrencyTypes.Where(c => c.Enabled && c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .Select(c => new AvailableCurrencySnapshot { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);

        return new RentableSpaceConfigSnapshot
        {
            FurnitureId = FurnitureId,
            IsConfigured = _terms is not null,
            Price = _terms?.Price ?? 0,
            CurrencyTypeId = _terms?.CurrencyTypeEntityId ?? 0,
            RentDurationSeconds = _terms?.RentDurationSeconds ?? 0,
            RequiresHc = _terms?.RequiresHc ?? false,
            AvailableCurrencies = currencies,
        };
    }

    public async Task<bool> ConfigureAsync(
        int actorPlayerId,
        bool isStaff,
        int price,
        int currencyTypeId,
        int rentDurationSeconds,
        bool requiresHc,
        CancellationToken ct
    )
    {
        await using TurboDbContext db = await dbCtxFactory.CreateDbContextAsync(ct);

        // Verify actor is the furniture owner (staff bypass allowed).
        int? furniOwnerId = await db
            .Furnitures.Where(f => f.Id == FurnitureId)
            .Select(f => (int?)f.PlayerEntityId)
            .FirstOrDefaultAsync(ct);

        if (!isStaff && furniOwnerId != actorPlayerId)
        {
            return false;
        }

        // Validate currency type exists.
        bool currencyExists = await db.CurrencyTypes.AnyAsync(
            c => c.Id == currencyTypeId && c.DeletedAt == null,
            ct
        );

        if (!currencyExists)
        {
            return false;
        }

        RentableSpaceTermsEntity? existing = await db.RentableSpaceTerms.FirstOrDefaultAsync(
            t => t.FurnitureEntityId == FurnitureId && t.DeletedAt == null,
            ct
        );

        if (existing is null)
        {
            db.RentableSpaceTerms.Add(
                new RentableSpaceTermsEntity
                {
                    FurnitureEntityId = FurnitureId,
                    Price = price,
                    CurrencyTypeEntityId = currencyTypeId,
                    RentDurationSeconds = rentDurationSeconds,
                    RequiresHc = requiresHc,
                    FurnitureEntity = null!,
                    CurrencyTypeEntity = null!,
                }
            );
        }
        else
        {
            existing.Price = price;
            existing.CurrencyTypeEntityId = currencyTypeId;
            existing.RentDurationSeconds = rentDurationSeconds;
            existing.RequiresHc = requiresHc;
        }

        await db.SaveChangesAsync(ct);

        // Reload _terms so subsequent GetStatusAsync reflects the new config.
        _terms = await db
            .RentableSpaceTerms.Include(t => t.CurrencyTypeEntity)
            .FirstOrDefaultAsync(
                t => t.FurnitureEntityId == FurnitureId && t.DeletedAt == null,
                ct
            );

        return true;
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await using TurboDbContext db = await dbCtxFactory.CreateDbContextAsync(ct);

        _space = await db
            .RoomRentableSpaces.Include(s => s.RenterPlayerEntity)
            .FirstOrDefaultAsync(
                s => s.FurnitureEntityId == FurnitureId && s.DeletedAt == null,
                ct
            );

        _roomId = await db
            .Furnitures.Where(f => f.Id == FurnitureId)
            .Select(f => f.RoomEntityId)
            .FirstOrDefaultAsync(ct);

        _terms = await db
            .RentableSpaceTerms.Include(t => t.CurrencyTypeEntity)
            .FirstOrDefaultAsync(
                t => t.FurnitureEntityId == FurnitureId && t.DeletedAt == null,
                ct
            );

        if (_space?.RenterPlayerEntityId is not null && _space.RentedUntil is not null)
        {
            _renterName = _space.RenterPlayerEntity?.Name ?? string.Empty;
            ScheduleExpiryTimer(_space.RentedUntil.Value);
        }
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _expiryTimer?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Returns all furniture tagged with this space back to the renter's inventory (DB),
    ///     clears the state row, and notifies the renter's presence grain.
    /// </summary>
    private async Task ClearRentalAsync(int renterId, CancellationToken ct)
    {
        _expiryTimer?.Dispose();
        _expiryTimer = null;

        try
        {
            await using TurboDbContext db = await dbCtxFactory.CreateDbContextAsync(ct);

            // Collect IDs of all furniture tagged to this space.
            List<int> taggedIds = await db
                .Furnitures.Where(f =>
                    f.RentableSpaceFurnitureEntityId == FurnitureId && f.DeletedAt == null
                )
                .Select(f => f.Id)
                .ToListAsync(ct);

            if (taggedIds.Count > 0)
            {
                // Clear the rentable-space tag so items are no longer associated.
                await db
                    .Furnitures.Where(f => taggedIds.Contains(f.Id))
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(f => f.RentableSpaceFurnitureEntityId, (int?)null),
                        ct
                    );

                // Remove each item from the live room grain so it disappears from the
                // map immediately, gets returned to its owner's inventory, and has its
                // RoomEntityId zeroed via the grain's dirty-flush pipeline.
                if (_roomId.HasValue)
                {
                    IRoomGrain roomGrain = grainFactory.GetRoomGrain(new RoomId(_roomId.Value));

                    await Task.WhenAll(
                        taggedIds.Select(id =>
                            roomGrain.RemoveItemByIdAsync(
                                ActionContext.System,
                                new RoomObjectId(id),
                                ct
                            )
                        )
                    );
                }
            }

            // Clear the state row in-place.
            RoomRentableSpaceEntity? entity = await db.RoomRentableSpaces.FirstOrDefaultAsync(
                s => s.FurnitureEntityId == FurnitureId && s.DeletedAt == null,
                ct
            );

            if (entity is not null)
            {
                entity.RenterPlayerEntityId = null;
                entity.RentedUntil = null;
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing rental for space furni {FurnitureId}", FurnitureId);
            return;
        }

        // Mirror in-memory state.
        if (_space is not null)
        {
            _space.RenterPlayerEntityId = null;
            _space.RentedUntil = null;
        }

        _renterName = string.Empty;
        await SetVisualStateAsync(0, CancellationToken.None);

        // Push an updated (free) status composer to the renter so the client UI refreshes.
        try
        {
            await grainFactory
                .GetPlayerPresenceGrain((long)renterId)
                .SendComposerAsync(
                    new RentableSpaceStatusMessageComposer
                    {
                        Rented = false,
                        CanRentErrorCode = _terms is not null
                            ? RentableSpaceRentFailedType.None
                            : RentableSpaceRentFailedType.Disabled,
                        RenterId = 0,
                        RenterName = string.Empty,
                        TimeRemaining = 0,
                        Price = _terms?.Price ?? 0,
                        CurrencyName = _terms?.CurrencyTypeEntity?.Name ?? string.Empty,
                    }
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Could not notify presence grain for renter {RenterId} on space expiry",
                renterId
            );
        }
    }

    private void ScheduleExpiryTimer(DateTime expiresAt)
    {
        _expiryTimer?.Dispose();

        TimeSpan delay = expiresAt - DateTime.UtcNow;
        if (delay <= TimeSpan.Zero)
        {
            // Already expired — fire immediately (next grain turn).
            delay = TimeSpan.FromMilliseconds(1);
        }

        _expiryTimer = this.RegisterGrainTimer<object?>(
            static (self, tickCt) => ((RentableSpaceGrain)self!).ExpireAsync(tickCt),
            this,
            delay,
            Timeout.InfiniteTimeSpan
        );
    }

    private async Task SetVisualStateAsync(int state, CancellationToken ct)
    {
        if (_roomId is null)
        {
            return;
        }

        try
        {
            await grainFactory
                .GetRoomGrain(new RoomId(_roomId.Value))
                .SetFloorItemStateAsync(new RoomObjectId(FurnitureId), state, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to set visual state {State} for rentable space furni {FurniId}",
                state,
                FurnitureId
            );
        }
    }
}
