using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Turbo.Database.Context;
using Turbo.Database.Entities.Furniture;
using Turbo.Primitives.Messages.Outgoing.Room.Furniture;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Wallet;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Grains;
using Turbo.Primitives.Rooms.Snapshots;

namespace Turbo.Players.Grains;

/// <summary>
///     Per-instance grain (key = furniture id) for one placed rentable-space item.
///     Serializes all rent / cancel / expire transitions; all DB mutations go through here.
/// </summary>
internal sealed class RentableSpaceGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    ILogger<RentableSpaceGrain> logger
) : Grain, IRentableSpaceGrain
{
    private IGrainTimer? _expiryTimer;
    private string _renterName = string.Empty;

    // Hot state — kept in sync with DB after every mutation.
    private RoomRentableSpaceEntity? _space;

    // Cold config (loaded once, never mutated at runtime).
    private RentableSpaceTermsEntity? _terms;
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

        // Upsert state row (insert first time, update in-place thereafter).
        if (_space is null)
        {
            FurnitureEntity? furniEntity = await db.Furnitures.FirstOrDefaultAsync(
                f => f.Id == FurnitureId,
                ct
            );

            if (furniEntity is null)
            {
                return (int)RentableSpaceRentFailedType.Generic;
            }

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

        string renterName =
            await db
                .Players.Where(p => p.Id == renterPlayerId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct)
            ?? string.Empty;

        _renterName = renterName;
        ScheduleExpiryTimer(rentedUntil);

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

        int? definitionId = await db
            .Furnitures.Where(f => f.Id == FurnitureId)
            .Select(f => (int?)f.FurnitureDefinitionEntityId)
            .FirstOrDefaultAsync(ct);

        if (definitionId.HasValue)
        {
            _terms = await db
                .RentableSpaceTerms.Include(t => t.CurrencyTypeEntity)
                .FirstOrDefaultAsync(
                    t => t.FurnitureDefinitionEntityId == definitionId.Value && t.DeletedAt == null,
                    ct
                );
        }

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

            // Bulk-return all tagged furniture to renter inventory (DATA-MODEL §3.3).
            await db
                .Furnitures.Where(f =>
                    f.RentableSpaceFurnitureEntityId == FurnitureId && f.DeletedAt == null
                )
                .ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(f => f.RoomEntityId, (int?)null)
                            .SetProperty(f => f.RentableSpaceFurnitureEntityId, (int?)null),
                    ct
                );

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

        _expiryTimer = this.RegisterGrainTimer(
            _ => ExpireAsync(CancellationToken.None),
            (object?)null,
            delay,
            Timeout.InfiniteTimeSpan
        );
    }
}
