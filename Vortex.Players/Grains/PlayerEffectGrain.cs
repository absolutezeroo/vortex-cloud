using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Players.Effects;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Avatareffect;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.Players.Grains;

/// <summary>Owns the <c>player_effects</c> table for one player. A DB-gateway grain (no persistent state,
/// like <see cref="PlayerBadgeGrain"/>): every call opens a fresh context. Timed effects expire two ways —
/// a best-effort grain timer for live disappearance while the grain is active, and a lazy purge run at the
/// start of every operation so the inventory and worn state are always correct on the next interaction or
/// login even if the grain had deactivated.</summary>
internal sealed class PlayerEffectGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    ILogger<PlayerEffectGrain> logger
) : Grain, IPlayerEffectGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<PlayerEffectGrain> _logger = logger;

    private IGrainTimer? _expiryTimer;

    private int OwnerId => (int)this.GetPrimaryKeyLong();

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        // Re-arm proactive expiry across grain reactivations so a timed effect still disappears live.
        await ArmNextExpiryAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public async Task<ImmutableArray<AvatarEffectSnapshot>> GetEffectsAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            // Login/list path: the returned list is authoritative, so purge silently (no expiry pushes
            // for effects the client never saw).
            await PurgeExpiredAsync(dbCtx, DateTime.UtcNow, ct).ConfigureAwait(true);

            return await BuildListAsync(dbCtx, ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load effects for player {PlayerId}", OwnerId);
            return ImmutableArray<AvatarEffectSnapshot>.Empty;
        }
    }

    public async Task AddEffectAsync(
        int effectId,
        int subType,
        int durationSeconds,
        CancellationToken ct
    )
    {
        if (effectId <= 0)
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            dbCtx.PlayerEffects.Add(
                new PlayerEffectEntity
                {
                    PlayerEntityId = OwnerId,
                    EffectId = effectId,
                    SubType = subType,
                    TotalDuration = Math.Max(0, durationSeconds),
                    ActivatedAt = null,
                    IsSelected = false,
                    PlayerEntity = null!,
                }
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(OwnerId);
            await presence
                .SendComposerAsync(
                    new AvatarEffectAddedMessageComposer
                    {
                        Type = effectId,
                        SubType = subType,
                        Duration = Math.Max(0, durationSeconds),
                        IsPermanent = durationSeconds <= 0,
                    }
                )
                .ConfigureAwait(true);
            await SendListAsync(presence, dbCtx, ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add effect {EffectId} for player {PlayerId}",
                effectId,
                OwnerId
            );
            throw; // let the catalog purchase auto-refund path see the failure
        }
    }

    public async Task ActivateEffectAsync(int effectId, CancellationToken ct)
    {
        if (effectId <= 0)
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            DateTime now = DateTime.UtcNow;
            IReadOnlyList<PlayerEffectEntity> expired = await PurgeExpiredAsync(dbCtx, now, ct)
                .ConfigureAwait(true);

            PlayerEffectEntity? inactive = await dbCtx
                .PlayerEffects.Where(e =>
                    e.PlayerEntityId == OwnerId && e.EffectId == effectId && e.ActivatedAt == null
                )
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(true);

            if (inactive is null)
            {
                await NotifyExpiredAsync(expired, dbCtx, ct).ConfigureAwait(true);
                return;
            }

            inactive.ActivatedAt = now;
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(OwnerId);
            await presence
                .SendComposerAsync(
                    new AvatarEffectActivatedMessageComposer
                    {
                        Type = effectId,
                        Duration = inactive.TotalDuration,
                        IsPermanent = inactive.TotalDuration <= 0,
                    }
                )
                .ConfigureAwait(true);

            foreach (int expiredId in expired.Select(e => e.EffectId).Distinct())
            {
                await presence
                    .SendComposerAsync(new AvatarEffectExpiredMessageComposer { Type = expiredId })
                    .ConfigureAwait(true);
            }

            await SendListAsync(presence, dbCtx, ct).ConfigureAwait(true);

            if (expired.Any(e => e.IsSelected))
            {
                await ClearWornInRoomAsync(ct).ConfigureAwait(true);
            }

            await ArmNextExpiryAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to activate effect {EffectId} for player {PlayerId}",
                effectId,
                OwnerId
            );
        }
    }

    public async Task<int> SelectEffectAsync(int effectId, CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            IReadOnlyList<PlayerEffectEntity> expired = await PurgeExpiredAsync(
                    dbCtx,
                    DateTime.UtcNow,
                    ct
                )
                .ConfigureAwait(true);

            List<PlayerEffectEntity> owned = await dbCtx
                .PlayerEffects.Where(e => e.PlayerEntityId == OwnerId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            int applied = 0;

            if (effectId > 0)
            {
                // Only an activated (or permanent) copy can be worn.
                PlayerEffectEntity? wearable = owned
                    .Where(e =>
                        e.EffectId == effectId && (e.ActivatedAt != null || e.TotalDuration <= 0)
                    )
                    .OrderBy(e => e.Id)
                    .FirstOrDefault();

                if (wearable is null)
                {
                    await NotifyExpiredAsync(expired, dbCtx, ct).ConfigureAwait(true);
                    return 0;
                }

                foreach (PlayerEffectEntity e in owned)
                {
                    e.IsSelected = e.Id == wearable.Id;
                }

                applied = effectId;
            }
            else
            {
                foreach (PlayerEffectEntity e in owned)
                {
                    e.IsSelected = false;
                }
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(OwnerId);
            await presence
                .SendComposerAsync(new AvatarEffectSelectedMessageComposer { Type = applied })
                .ConfigureAwait(true);

            foreach (int expiredId in expired.Select(e => e.EffectId).Distinct())
            {
                await presence
                    .SendComposerAsync(new AvatarEffectExpiredMessageComposer { Type = expiredId })
                    .ConfigureAwait(true);
            }

            if (expired.Count > 0)
            {
                await SendListAsync(presence, dbCtx, ct).ConfigureAwait(true);
            }

            return applied;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to select effect {EffectId} for player {PlayerId}",
                effectId,
                OwnerId
            );
            return 0;
        }
    }

    public async Task<int> GetSelectedEffectAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            // Purge silently (no room callback): this runs while the room grain awaits us during room
            // entry, so calling back into it would risk a reentrancy deadlock. If the worn effect had
            // expired it is simply dropped here and returns 0; the timer/next interaction pushes the ack.
            await PurgeExpiredAsync(dbCtx, DateTime.UtcNow, ct).ConfigureAwait(true);

            return await dbCtx
                .PlayerEffects.Where(e => e.PlayerEntityId == OwnerId && e.IsSelected)
                .Select(e => e.EffectId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read worn effect for player {PlayerId}", OwnerId);
            return 0;
        }
    }

    // --- helpers ---------------------------------------------------------------------------------

    private async Task<ImmutableArray<AvatarEffectSnapshot>> BuildListAsync(
        VortexDbContext dbCtx,
        CancellationToken ct
    )
    {
        List<PlayerEffectEntity> rows = await dbCtx
            .PlayerEffects.AsNoTracking()
            .Where(e => e.PlayerEntityId == OwnerId)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        return AvatarEffectListBuilder.Build(
            rows.Select(e => new PlayerEffectRow(
                e.EffectId,
                e.SubType,
                e.TotalDuration,
                e.ActivatedAt,
                e.IsSelected
            )),
            DateTime.UtcNow
        );
    }

    private async Task SendListAsync(
        IPlayerPresenceGrain presence,
        VortexDbContext dbCtx,
        CancellationToken ct
    )
    {
        ImmutableArray<AvatarEffectSnapshot> effects = await BuildListAsync(dbCtx, ct)
            .ConfigureAwait(true);
        await presence
            .SendComposerAsync(new AvatarEffectsMessageComposer { Effects = effects })
            .ConfigureAwait(true);
    }

    /// <summary>Deletes activated, timed effects whose countdown has elapsed. Returns the removed rows so
    /// the caller can push <c>AvatarEffectExpired</c> and un-wear in the room. Saves within this context.</summary>
    private async Task<IReadOnlyList<PlayerEffectEntity>> PurgeExpiredAsync(
        VortexDbContext dbCtx,
        DateTime now,
        CancellationToken ct
    )
    {
        List<PlayerEffectEntity> active = await dbCtx
            .PlayerEffects.Where(e =>
                e.PlayerEntityId == OwnerId && e.ActivatedAt != null && e.TotalDuration > 0
            )
            .ToListAsync(ct)
            .ConfigureAwait(true);

        List<PlayerEffectEntity> expired = active
            .Where(e => e.ActivatedAt!.Value.AddSeconds(e.TotalDuration) <= now)
            .ToList();

        if (expired.Count > 0)
        {
            dbCtx.PlayerEffects.RemoveRange(expired);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }

        return expired;
    }

    private async Task NotifyExpiredAsync(
        IReadOnlyList<PlayerEffectEntity> expired,
        VortexDbContext dbCtx,
        CancellationToken ct
    )
    {
        if (expired.Count == 0)
        {
            return;
        }

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(OwnerId);

        foreach (int effectId in expired.Select(e => e.EffectId).Distinct())
        {
            await presence
                .SendComposerAsync(new AvatarEffectExpiredMessageComposer { Type = effectId })
                .ConfigureAwait(true);
        }

        await SendListAsync(presence, dbCtx, ct).ConfigureAwait(true);

        if (expired.Any(e => e.IsSelected))
        {
            await ClearWornInRoomAsync(ct).ConfigureAwait(true);
        }
    }

    private async Task ClearWornInRoomAsync(CancellationToken ct)
    {
        RoomPointerSnapshot room = await _grainFactory
            .GetPlayerPresenceGrain(OwnerId)
            .GetActiveRoomAsync()
            .ConfigureAwait(true);

        if (room.RoomId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetRoomGrain(room.RoomId)
            .SetAvatarEffectAsync(
                ActionContext.CreateForPlayer(PlayerId.Parse(OwnerId), room.RoomId),
                0,
                ct
            )
            .ConfigureAwait(true);
    }

    private async Task ArmNextExpiryAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerEffectEntity> active = await dbCtx
                .PlayerEffects.AsNoTracking()
                .Where(e =>
                    e.PlayerEntityId == OwnerId && e.ActivatedAt != null && e.TotalDuration > 0
                )
                .ToListAsync(ct)
                .ConfigureAwait(true);

            _expiryTimer?.Dispose();
            _expiryTimer = null;

            if (active.Count == 0)
            {
                return;
            }

            DateTime soonest = active.Min(e => e.ActivatedAt!.Value.AddSeconds(e.TotalDuration));
            TimeSpan delay = soonest - DateTime.UtcNow;

            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.FromMilliseconds(1);
            }

            _expiryTimer = this.RegisterGrainTimer<object?>(
                static (self, tickCt) => ((PlayerEffectGrain)self!).ExpireDueAsync(tickCt),
                this,
                delay,
                Timeout.InfiniteTimeSpan
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to arm effect expiry timer for player {PlayerId}",
                OwnerId
            );
        }
    }

    private async Task ExpireDueAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            IReadOnlyList<PlayerEffectEntity> expired = await PurgeExpiredAsync(
                    dbCtx,
                    DateTime.UtcNow,
                    ct
                )
                .ConfigureAwait(true);

            await NotifyExpiredAsync(expired, dbCtx, ct).ConfigureAwait(true);
            await ArmNextExpiryAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expire effects for player {PlayerId}", OwnerId);
        }
    }
}
