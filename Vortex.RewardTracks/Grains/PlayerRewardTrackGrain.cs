using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.RewardTracks;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Grains;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.Primitives.Server.Grains;
using Vortex.Protocol.Messages.Outgoing.RewardTracks;
using Vortex.RewardTracks.Progression;
using Vortex.RewardTracks.Rewards;

namespace Vortex.RewardTracks.Grains;

/// <summary>
/// One player's reward-track state, across every track at once.
/// </summary>
/// <remarks>
/// <para>
/// The single mutation boundary the design turns on. Points, task progress, claims and premium all
/// change here and nowhere else, and Orleans runs one turn at a time per player — so the
/// read-decide-write that each of those is stays atomic without a lock, and a player spamming a
/// claim button gets a second turn that finds the work already done.
/// </para>
/// <para>
/// There is no "current track". The client picks one to look at; this progresses all of them at
/// once, and one signal routinely advances tasks on several.
/// </para>
/// </remarks>
internal sealed partial class PlayerRewardTrackGrain(
    IGrainFactory grainFactory,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IRewardTrackCatalog catalog,
    RewardGrantPipeline rewards,
    IEventPublisher events,
    ICommerceJournal journal,
    ILogger<PlayerRewardTrackGrain> logger
) : Grain, IPlayerRewardTrackGrain
{
    private const string EnabledKey = "reward_tracks.enabled";

    private readonly Dictionary<string, TrackState> _tracks = new(StringComparer.Ordinal);

    private bool _featureEnabled = true;

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    private IPlayerPresenceGrain Presence => grainFactory.GetPlayerPresenceGrain(PlayerId);

    /// <summary>One player's stored state on one track, in the shape the turn actually mutates.</summary>
    private sealed class TrackState
    {
        public int Points { get; set; }
        public bool PremiumUnlocked { get; set; }
        public DateTime? PremiumUnlockedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int ContentVersion { get; set; }
        public Dictionary<string, TaskState> Tasks { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ClaimedPrizeIds { get; } = new(StringComparer.Ordinal);
    }

    private sealed class TaskState
    {
        public int ProgressCount { get; set; }
        public int HighestPaidLevelIndex { get; set; } = -1;
        public string DistinctKeys { get; set; } = string.Empty;

        /// <summary>Where this player stands in the task's sequence; always 0 for a plain task.</summary>
        public int CurrentStep { get; set; }

        /// <summary>What each satisfied step matched, for the steps that point back at them.</summary>
        public string CapturedFacts { get; set; } = string.Empty;
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await ReadFeatureFlagAsync().ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public Task<ImmutableArray<RewardTrackViewSnapshot>> GetTracksAsync(CancellationToken ct) =>
        BuildVisibleTracksAsync(ct);

    public async Task PushTracksAsync(bool reload, CancellationToken ct)
    {
        ImmutableArray<RewardTrackViewSnapshot> tracks = _featureEnabled
            ? await BuildVisibleTracksAsync(ct).ConfigureAwait(true)
            : [];

        await Presence
            .SendComposerAsync(
                new RewardTracksMessageComposer
                {
                    Disabled = !_featureEnabled,
                    Tracks = tracks,
                    Reload = reload,
                }
            )
            .ConfigureAwait(true);
    }

    public Task<bool> HasUnclaimedRewardsAsync(CancellationToken ct)
    {
        if (!_featureEnabled)
        {
            return Task.FromResult(false);
        }

        DateTime now = DateTime.UtcNow;

        // Answered from state already in memory: no track is rebuilt, no view is folded. The red
        // dot is asked for far more often than the track list is drawn, and it only needs to know
        // whether any prize is over the line and untaken.
        foreach ((string trackId, TrackState state) in _tracks)
        {
            if (
                !catalog.TryGetTrack(trackId, out RewardTrackDefinitionSnapshot? track)
                || !track.AcceptsClaimsAt(now)
            )
            {
                continue;
            }

            foreach (RewardTrackPrizeDefinitionSnapshot prize in track.Prizes)
            {
                if (state.ClaimedPrizeIds.Contains(prize.PrizeId))
                {
                    continue;
                }

                if (
                    state.Points >= prize.RequiredPoints
                    && (!prize.Premium || state.PremiumUnlocked)
                )
                {
                    return Task.FromResult(true);
                }
            }
        }

        return Task.FromResult(false);
    }

    public Task<ImmutableArray<PlayerRewardTrackStateSnapshot>> GetRawStateAsync(
        CancellationToken ct
    ) => Task.FromResult(_tracks.Select(kv => ToSnapshot(kv.Key, kv.Value)).ToImmutableArray());

    public async Task<bool> ResetTrackAsync(string trackId, CancellationToken ct)
    {
        if (!_tracks.Remove(trackId))
        {
            return false;
        }

        await using (
            VortexDbContext db = await dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true)
        )
        {
            // Tracked removes rather than three ExecuteDelete calls: the state, the task progress
            // and the claims have to go together or the player is left with claims on points they
            // no longer have.
            db.PlayerRewardTracks.RemoveRange(
                await db
                    .PlayerRewardTracks.Where(t =>
                        t.PlayerEntityId == PlayerId && t.TrackId == trackId
                    )
                    .ToListAsync(ct)
                    .ConfigureAwait(true)
            );
            db.PlayerRewardTrackTasks.RemoveRange(
                await db
                    .PlayerRewardTrackTasks.Where(t =>
                        t.PlayerEntityId == PlayerId && t.TrackId == trackId
                    )
                    .ToListAsync(ct)
                    .ConfigureAwait(true)
            );
            db.PlayerRewardTrackClaims.RemoveRange(
                await db
                    .PlayerRewardTrackClaims.Where(c =>
                        c.PlayerEntityId == PlayerId && c.TrackId == trackId
                    )
                    .ToListAsync(ct)
                    .ConfigureAwait(true)
            );

            await db.SaveChangesAsync(ct).ConfigureAwait(true);
        }

        logger.LogWarning(
            "Reset player {PlayerId}'s progress on reward track {TrackId}.",
            PlayerId,
            trackId
        );

        await PushTracksAsync(reload: true, ct).ConfigureAwait(true);

        return true;
    }

    public async Task InvalidateAsync(CancellationToken ct)
    {
        await ReadFeatureFlagAsync().ConfigureAwait(true);
        await PushTracksAsync(reload: true, ct).ConfigureAwait(true);
    }

    /// <summary>
    /// Every track this player may see, resolved against their state.
    /// </summary>
    /// <remarks>
    /// The unlock facts are gathered once for the whole list rather than per track: eight chapters
    /// of one campaign would otherwise ask for the same badge list eight times.
    /// </remarks>
    private async Task<ImmutableArray<RewardTrackViewSnapshot>> BuildVisibleTracksAsync(
        CancellationToken ct
    )
    {
        DateTime now = DateTime.UtcNow;
        UnlockFacts facts = await GatherUnlockFactsAsync(ct).ConfigureAwait(true);
        List<RewardTrackViewSnapshot> views = [];

        foreach (RewardTrackDefinitionSnapshot track in catalog.Tracks)
        {
            bool hasProgress = _tracks.ContainsKey(track.TrackId);

            if (!track.IsVisibleAt(now))
            {
                continue;
            }

            // A hidden track is served to whoever already has progress on it and to nobody else:
            // that is what makes a track testable in production without showing it to the hotel.
            if (track.Hidden && !hasProgress)
            {
                continue;
            }

            if (!hasProgress && !TrackGatingRules.IsUnlocked(track, facts))
            {
                continue;
            }

            views.Add(TrackViewBuilder.Build(track, StateFor(track)));
        }

        return [.. views.OrderBy(v => OrderOf(v.TrackId))];
    }

    private int OrderOf(string trackId) =>
        catalog.TryGetTrack(trackId, out RewardTrackDefinitionSnapshot? track)
            ? track.SortOrder
            : int.MaxValue;

    /// <summary>
    /// The player's state on a track, or an empty one. Does not create a stored row: a player who
    /// has never touched a track has no rows, and the first thing that advances writes them.
    /// </summary>
    private PlayerRewardTrackStateSnapshot StateFor(RewardTrackDefinitionSnapshot track) =>
        _tracks.TryGetValue(track.TrackId, out TrackState? state)
            ? ToSnapshot(track.TrackId, state)
            : new PlayerRewardTrackStateSnapshot
            {
                TrackId = track.TrackId,
                Points = 0,
                PremiumUnlocked = false,
                Tasks = [],
                ClaimedPrizeIds = [],
                ContentVersion = track.ContentVersion,
            };

    private static PlayerRewardTrackStateSnapshot ToSnapshot(string trackId, TrackState state) =>
        new()
        {
            TrackId = trackId,
            Points = state.Points,
            PremiumUnlocked = state.PremiumUnlocked,
            PremiumUnlockedAtUtc = state.PremiumUnlockedAt,
            CompletedAtUtc = state.CompletedAt,
            ContentVersion = state.ContentVersion,
            Tasks =
            [
                .. state.Tasks.Select(kv => new PlayerTaskProgressSnapshot
                {
                    TaskId = kv.Key,
                    ProgressCount = kv.Value.ProgressCount,
                    HighestPaidLevelIndex = kv.Value.HighestPaidLevelIndex,
                }),
            ],
            ClaimedPrizeIds = [.. state.ClaimedPrizeIds],
        };

    /// <summary>
    /// Everything an unlock condition might ask about, fetched once.
    /// </summary>
    /// <remarks>
    /// Only what the published content actually uses is fetched: a hotel whose tracks are all
    /// <c>Always</c> makes no calls at all, which is the common case and the one worth being cheap.
    /// </remarks>
    private async Task<UnlockFacts> GatherUnlockFactsAsync(CancellationToken ct)
    {
        HashSet<string> completed = new(StringComparer.Ordinal);
        HashSet<string> claimedKeys = new(StringComparer.Ordinal);

        foreach ((string trackId, TrackState state) in _tracks)
        {
            if (state.CompletedAt is not null)
            {
                completed.Add(trackId);
            }

            foreach (string prizeId in state.ClaimedPrizeIds)
            {
                claimedKeys.Add($"{trackId}:{prizeId}");
            }
        }

        bool needsBadges = false;
        bool needsAge = false;
        List<string> flagKeys = [];

        foreach (RewardTrackDefinitionSnapshot track in catalog.Tracks)
        {
            switch (track.UnlockKind)
            {
                case RewardTrackUnlockKind.BadgeOwned:
                    needsBadges = true;
                    break;
                case RewardTrackUnlockKind.AccountAgeDays:
                    needsAge = true;
                    break;
                case RewardTrackUnlockKind.FeatureFlag:
                    flagKeys.Add(track.UnlockValue);
                    break;
            }
        }

        HashSet<string> badges = new(StringComparer.Ordinal);
        int ageDays = 0;
        Dictionary<string, bool> flags = new(StringComparer.Ordinal);

        if (needsBadges)
        {
            foreach (
                PlayerBadgeSnapshot badge in await grainFactory
                    .GetPlayerBadgeGrain(PlayerId)
                    .GetBadgesAsync(ct)
                    .ConfigureAwait(true)
            )
            {
                badges.Add(badge.BadgeCode);
            }
        }

        if (needsAge)
        {
            PlayerSummarySnapshot summary = await grainFactory
                .GetPlayerGrain(PlayerId)
                .GetSummaryAsync(ct)
                .ConfigureAwait(true);

            ageDays = (int)Math.Max(0, (DateTime.UtcNow - summary.CreatedAt).TotalDays);
        }

        if (flagKeys.Count > 0)
        {
            IServerConfigGrain config = grainFactory.GetServerConfigGrain();

            foreach (string key in flagKeys.Distinct(StringComparer.Ordinal))
            {
                flags[key] = await config.GetBoolAsync(key, false).ConfigureAwait(true);
            }
        }

        return new UnlockFacts(completed, claimedKeys, badges, ageDays, flags);
    }

    private async Task ReadFeatureFlagAsync()
    {
        try
        {
            _featureEnabled = await grainFactory
                .GetServerConfigGrain()
                .GetBoolAsync(EnabledKey, true)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // Left on. A config read that failed is not a reason to hide every campaign in the
            // hotel from every player.
            logger.LogError(ex, "Failed to read the reward-track feature flag; leaving it on.");
        }
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext db = await dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerRewardTrackEntity> trackRows = await db
                .PlayerRewardTracks.AsNoTracking()
                .Where(t => t.PlayerEntityId == PlayerId && t.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<PlayerRewardTrackTaskEntity> taskRows = await db
                .PlayerRewardTrackTasks.AsNoTracking()
                .Where(t => t.PlayerEntityId == PlayerId && t.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<PlayerRewardTrackClaimEntity> claimRows = await db
                .PlayerRewardTrackClaims.AsNoTracking()
                .Where(c => c.PlayerEntityId == PlayerId && c.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            _tracks.Clear();

            foreach (PlayerRewardTrackEntity row in trackRows)
            {
                _tracks[row.TrackId] = new TrackState
                {
                    Points = row.Points,
                    PremiumUnlocked = row.PremiumUnlocked,
                    PremiumUnlockedAt = row.PremiumUnlockedAt,
                    CompletedAt = row.CompletedAt,
                    ContentVersion = row.ContentVersion,
                };
            }

            foreach (PlayerRewardTrackTaskEntity row in taskRows)
            {
                TrackState state = EnsureTrack(row.TrackId);

                state.Tasks[row.TaskId] = new TaskState
                {
                    ProgressCount = row.ProgressCount,
                    HighestPaidLevelIndex = row.HighestPaidLevelIndex,
                    DistinctKeys = row.DistinctKeys,
                    CurrentStep = row.CurrentStep,
                    CapturedFacts = row.CapturedFacts,
                };
            }

            foreach (PlayerRewardTrackClaimEntity row in claimRows)
            {
                EnsureTrack(row.TrackId).ClaimedPrizeIds.Add(row.PrizeId);
            }
        }
        catch (Exception ex)
        {
            // Nothing is faked into existence. An empty map would read as a player who has done
            // nothing, and the next signal would start their progress again from zero over the top
            // of rows that are still there.
            logger.LogError(
                ex,
                "Failed to load reward-track state for player {PlayerId}.",
                PlayerId
            );
        }
    }

    /// <summary>
    /// The in-memory state for a track, created if this is the first thing to touch it. A task row
    /// or a claim row for a track with no state row is possible after a partial reset, and reading
    /// one must not throw.
    /// </summary>
    private TrackState EnsureTrack(string trackId)
    {
        if (!_tracks.TryGetValue(trackId, out TrackState? state))
        {
            state = new TrackState();
            _tracks[trackId] = state;
        }

        return state;
    }
}
