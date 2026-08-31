using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Fishing;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Fishing.Grains;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Fishing.Grains;

/// <summary>
/// One player's fishing progression.
/// </summary>
/// <remarks>
/// <para>
/// Reconstructed from Habbo Origins, which has no client dump — see the client's
/// <c>docs/vortex-original/fishing.md</c>.
/// </para>
/// <para>
/// Every write here is a read-modify-write of the same row, and catches arrive on a timer that does
/// not wait for the previous one to finish. The grain being single-threaded per player is what makes
/// that safe; nothing below takes a lock, because the grain boundary is the lock.
/// </para>
/// <para>
/// <strong>Two progressions.</strong> The same XP feeds the fishing level, which unlocks zones, and
/// the rod quality, which raises the multipliers and the Hook Havoc chance. They are separate
/// counters walked against separate curves — collapsing them into one number was the second-biggest
/// error in this system's first design.
/// </para>
/// </remarks>
internal sealed class FishingPlayerGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IFurnitureDefinitionProvider definitionProvider,
    ILogger<FishingPlayerGrain> logger
) : Grain, IFishingPlayerGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IFurnitureDefinitionProvider _definitionProvider = definitionProvider;
    private readonly ILogger<FishingPlayerGrain> _logger = logger;

    private PlayerId PlayerId => new((int)this.GetPrimaryKeyLong());

    public async Task<FishingPlayerStateSnapshot> GetStateAsync(
        int sessionCatchCount,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        FishingPlayerStateEntity state = await LoadOrCreateAsync(dbCtx, ct).ConfigureAwait(true);
        FishingSettingsSnapshot settings = await _grainFactory
            .GetFishingDefinitionsGrain()
            .GetSettingsAsync(ct)
            .ConfigureAwait(true);

        return ToSnapshot(state, settings, sessionCatchCount);
    }

    public async Task<ImmutableArray<FishingRecordSnapshot>> GetRecordsAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        FishingRecordEntity[] records = await dbCtx
            .FishingRecords.AsNoTracking()
            .Where(record => record.PlayerId == PlayerId.Value && record.DeletedAt == null)
            .ToArrayAsync(ct)
            .ConfigureAwait(true);

        return [.. records.Select(ToSnapshot)];
    }

    public async Task<FishingRecordSnapshot?> FindRecordAsync(int recordId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        // The player id is part of the query, not checked afterwards: a record id is guessable, and
        // this is what stops one player mounting another's catch.
        FishingRecordEntity? record = await dbCtx
            .FishingRecords.AsNoTracking()
            .FirstOrDefaultAsync(
                row =>
                    row.Id == recordId && row.PlayerId == PlayerId.Value && row.DeletedAt == null,
                ct
            )
            .ConfigureAwait(true);

        return record is null ? null : ToSnapshot(record);
    }

    public async Task<bool> MountRecordAsync(int recordId, CancellationToken ct)
    {
        FishingRecordSnapshot? record = await FindRecordAsync(recordId, ct).ConfigureAwait(true);

        if (record is null)
        {
            return false;
        }

        FishingDefinitionsSnapshot definitions = await _grainFactory
            .GetFishingDefinitionsGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        if (string.IsNullOrEmpty(definitions.Settings.TrophyFurniClass))
        {
            _logger.LogDebug("No trophy furniture is configured; nothing to mount.");

            return false;
        }

        FurnitureDefinitionSnapshot? trophy = _definitionProvider.TryGetDefinitionByName(
            definitions.Settings.TrophyFurniClass
        );

        if (trophy is null)
        {
            // Configured but absent from furnidata. Worth saying out loud: from the player's side it
            // is indistinguishable from a button that does nothing.
            _logger.LogWarning(
                "Trophy furniture {ClassName} is configured but is in no furnidata",
                definitions.Settings.TrophyFurniClass
            );

            return false;
        }

        FishSpeciesSnapshot? species = definitions.Species.FirstOrDefault(entry =>
            entry.Id == record.SpeciesId
        );

        // `name<TAB>date<TAB>text`, which is what the client's trophy widget splits on — see
        // FurnitureTrophyWidgetHandler. The species travels as its localisation key, not as a display
        // string, so a trophy mounted in one language reads correctly in another.
        string legacyData = string.Join(
            '\t',
            PlayerId.Value.ToString(CultureInfo.InvariantCulture),
            DateTimeOffset
                .FromUnixTimeSeconds(record.BestAt)
                .UtcDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            $"{species?.NameKey ?? string.Empty}\t{record.BestWeight}"
        );

        await _grainFactory
            .GetInventoryGrain(PlayerId)
            .GrantFurnitureWithLegacyStuffDataAsync(trophy.Id, legacyData, ct)
            .ConfigureAwait(true);

        return true;
    }

    public async Task<FishingCatchOutcome> ApplyCatchAsync(
        FishingCatchProposal proposal,
        CancellationToken ct
    )
    {
        FishingDefinitionsSnapshot definitions = await _grainFactory
            .GetFishingDefinitionsGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            FishingPlayerStateEntity state = await LoadOrCreateAsync(dbCtx, ct)
                .ConfigureAwait(true);

            int cap = definitions.Settings.DailyCurrencyCap;
            int currencyGranted =
                cap <= 0
                    ? proposal.Currency
                    : Math.Clamp(cap - state.CurrencyEarnedToday, 0, proposal.Currency);

            state.Currency += currencyGranted;
            state.CurrencyEarnedToday += currencyGranted;
            state.FishingXp += proposal.Xp;
            state.RodXp += proposal.Xp;
            state.TotalCatches++;

            if (proposal.Golden)
            {
                state.GoldenCatches++;
            }

            int fishingLevel = LevelFor(definitions.Levels, state.FishingXp, state.FishingLevel);
            int rodQuality = QualityFor(definitions.RodTiers, state.RodXp, state.RodQuality);
            int newFishingLevel = fishingLevel > state.FishingLevel ? fishingLevel : 0;
            int newRodQuality = rodQuality > state.RodQuality ? rodQuality : 0;

            state.FishingLevel = fishingLevel;
            state.RodQuality = rodQuality;

            FishingRecordEntity record = await UpsertRecordAsync(dbCtx, proposal, ct)
                .ConfigureAwait(true);

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            return new FishingCatchOutcome
            {
                RecordId = record.Id,
                XpGranted = proposal.Xp,
                CurrencyGranted = currencyGranted,
                NewFishingLevel = newFishingLevel,
                NewRodQuality = newRodQuality,
                DailyCapReached = cap > 0 && state.CurrencyEarnedToday >= cap,
            };
        }
        catch (Exception ex)
        {
            // A catch that cannot be banked is not a catch. Answering an empty outcome makes the
            // session push a zero-reward result rather than silently crediting a player from a write
            // that never landed.
            _logger.LogError(ex, "Failed to bank a fishing catch for player {PlayerId}", PlayerId);

            return new FishingCatchOutcome
            {
                RecordId = 0,
                XpGranted = 0,
                CurrencyGranted = 0,
                NewFishingLevel = 0,
                NewRodQuality = 0,
                DailyCapReached = false,
            };
        }
    }

    public async Task PushStateAsync(int sessionCatchCount, CancellationToken ct)
    {
        FishingPlayerStateSnapshot state = await GetStateAsync(sessionCatchCount, ct)
            .ConfigureAwait(true);
        ImmutableArray<FishingRecordSnapshot> records = await GetRecordsAsync(ct)
            .ConfigureAwait(true);

        await _grainFactory
            .GetPlayerPresenceGrain(PlayerId)
            .SendComposerAsync(
                BuildStateComposer(state),
                new VortexFishingRecordsMessageComposer { Records = records }
            )
            .ConfigureAwait(true);
    }

    /// <summary>
    /// Shared with the login push so a player receives the same message whether they just logged in
    /// or just landed a fish.
    /// </summary>
    public static VortexFishingPlayerStateMessageComposer BuildStateComposer(
        FishingPlayerStateSnapshot state
    ) =>
        new()
        {
            FishingLevel = state.FishingLevel,
            FishingXp = state.FishingXp,
            RodQuality = state.RodQuality,
            RodXp = state.RodXp,
            Currency = state.Currency,
            CurrencyEarnedToday = state.CurrencyEarnedToday,
            DailyCap = state.DailyCap,
            SessionCatchCount = state.SessionCatchCount,
            CollectibleIds = state.CollectibleIds,
        };

    /// <summary>
    /// Reads the row, creating it on first sight, and clears the day's counter when it belongs to an
    /// earlier date.
    /// </summary>
    /// <remarks>
    /// Resetting on read is what removes the need for a midnight job: a hotel that was down over
    /// midnight comes back with every cap already clear, because nobody had read a stale counter
    /// yet. The row is tracked, so a caller that goes on to save gets the reset persisted with it.
    /// </remarks>
    private async Task<FishingPlayerStateEntity> LoadOrCreateAsync(
        VortexDbContext dbCtx,
        CancellationToken ct
    )
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        // A session that is not bound to a player yet carries -1, and a caller that passes it here
        // is a bug in that caller — but it is a bug that writes a row and then goes quiet, so it
        // says so and refuses rather than persisting nonsense. This is exactly how the login
        // bootstrap's use of the dispatch-time `ctx.PlayerId` was found: a fishing_player_state row
        // for player -1, and a real player who received no state at all.
        if (PlayerId.Value <= 0)
        {
            _logger.LogWarning(
                "Fishing state requested for player id {PlayerId}; refusing to create a row for an unbound session.",
                PlayerId.Value
            );

            return new FishingPlayerStateEntity
            {
                PlayerId = PlayerId.Value,
                CurrencyEarnedOn = today,
            };
        }

        FishingPlayerStateEntity? state = await dbCtx
            .FishingPlayerState.FirstOrDefaultAsync(row => row.PlayerId == PlayerId.Value, ct)
            .ConfigureAwait(true);

        if (state is null)
        {
            state = new FishingPlayerStateEntity
            {
                PlayerId = PlayerId.Value,
                CurrencyEarnedOn = today,
            };

            dbCtx.FishingPlayerState.Add(state);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            return state;
        }

        if (state.CurrencyEarnedOn != today)
        {
            state.CurrencyEarnedOn = today;
            state.CurrencyEarnedToday = 0;
        }

        return state;
    }

    /// <summary>
    /// Writes the Fishopedia row for one catch, creating it the first time the species is caught.
    /// </summary>
    /// <remarks>
    /// The new row is saved immediately because the caller needs its id — that id is what the client
    /// later names to mount the catch as a trophy, and it does not exist until the insert lands.
    /// </remarks>
    private async Task<FishingRecordEntity> UpsertRecordAsync(
        VortexDbContext dbCtx,
        FishingCatchProposal proposal,
        CancellationToken ct
    )
    {
        FishingRecordEntity? record = await dbCtx
            .FishingRecords.FirstOrDefaultAsync(
                row =>
                    row.PlayerId == PlayerId.Value
                    && row.SpeciesId == proposal.SpeciesId
                    && row.DeletedAt == null,
                ct
            )
            .ConfigureAwait(true);

        if (record is null)
        {
            record = new FishingRecordEntity
            {
                PlayerId = PlayerId.Value,
                SpeciesId = proposal.SpeciesId,
                BestWeight = proposal.Weight,
                CaughtCount = 1,
                BestAt = DateTime.UtcNow,
            };

            dbCtx.FishingRecords.Add(record);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            return record;
        }

        record.CaughtCount++;

        if (proposal.Weight > record.BestWeight)
        {
            record.BestWeight = proposal.Weight;
            record.BestAt = DateTime.UtcNow;
        }

        return record;
    }

    /// <summary>
    /// The highest level whose threshold the XP has reached.
    /// </summary>
    /// <remarks>
    /// Walked by threshold rather than indexed by level, so a curve with gaps in it still works and
    /// an operator can insert a level without renumbering the ones above. An empty curve leaves the
    /// player where they are rather than demoting them to zero.
    /// </remarks>
    private static int LevelFor(IReadOnlyList<FishingLevelSnapshot> curve, int xp, int currentLevel)
    {
        int level = currentLevel;

        foreach (FishingLevelSnapshot step in curve)
        {
            if (xp >= step.XpThreshold && step.Level > level)
            {
                level = step.Level;
            }
        }

        return level;
    }

    /// <summary>The same walk over the rod curve, which is a different table and a different XP.</summary>
    private static int QualityFor(
        IReadOnlyList<FishingRodLevelSnapshot> tiers,
        int xp,
        int currentQuality
    )
    {
        int quality = currentQuality;

        foreach (FishingRodLevelSnapshot tier in tiers)
        {
            if (xp >= tier.XpThreshold && tier.Quality > quality)
            {
                quality = tier.Quality;
            }
        }

        return quality;
    }

    private static FishingPlayerStateSnapshot ToSnapshot(
        FishingPlayerStateEntity state,
        FishingSettingsSnapshot settings,
        int sessionCatchCount
    ) =>
        new()
        {
            FishingLevel = state.FishingLevel,
            FishingXp = state.FishingXp,
            RodQuality = state.RodQuality,
            RodXp = state.RodXp,
            Currency = state.Currency,
            CurrencyEarnedToday = state.CurrencyEarnedToday,
            DailyCap = settings.DailyCurrencyCap,
            SessionCatchCount = sessionCatchCount,
            // ponytail: the bottles, statues and badge are not modelled yet — no table names which
            // furni or badge ids count as fishing collectibles, so there is nothing honest to read.
            // The client renders an empty list as "none owned", which is true of every player today.
            // Upgrade path: a fishing_collectibles table joined against the inventory and the badge
            // list, read here.
            CollectibleIds = [],
            TotalCatches = state.TotalCatches,
            GoldenCatches = state.GoldenCatches,
        };

    private static FishingRecordSnapshot ToSnapshot(FishingRecordEntity record) =>
        new()
        {
            SpeciesId = record.SpeciesId,
            BestWeight = record.BestWeight,
            CaughtCount = record.CaughtCount,
            BestAt = (int)new DateTimeOffset(record.BestAt, TimeSpan.Zero).ToUnixTimeSeconds(),
        };
}
