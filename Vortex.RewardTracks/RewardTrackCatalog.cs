using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.RewardTracks;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.RewardTracks;

/// <summary>
/// Every reward-track definition, held in memory, plus the action index the event bridge asks
/// before it does anything else.
/// </summary>
/// <remarks>
/// <para>
/// The index is what makes this design scale past one campaign. Room entries, chat lines and
/// furniture placements arrive constantly; without it, each one would reach a grain and scan every
/// task of every published track to find out it was of no interest.
/// <see cref="IsActionInteresting"/> answers from a hash set on the calling thread, so an action
/// no content mentions costs a lookup and stops there.
/// </para>
/// <para>
/// Loaded with the other reference caches at startup and reloaded by the admin service after a
/// content write. Never mutated by gameplay.
/// </para>
/// </remarks>
internal sealed class RewardTrackCatalog(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    ILogger<RewardTrackCatalog> logger
) : IRewardTrackCatalog, IReferenceDataProvider
{
    private ImmutableArray<RewardTrackDefinitionSnapshot> _tracks = [];
    private Index _index = Index.Empty;

    public int LoadStage => 0;

    public ImmutableArray<RewardTrackDefinitionSnapshot> Tracks => _tracks;

    public bool TryGetTrack(
        string trackId,
        [NotNullWhen(true)] out RewardTrackDefinitionSnapshot? track
    ) => _index.ByTrackId.TryGetValue(trackId, out track);

    public bool IsActionInteresting(string actionCode) => _index.Actions.Contains(actionCode);

    public ImmutableArray<RewardTrackTaskRef> TasksFor(string actionCode) =>
        _index.TasksByAction.TryGetValue(actionCode, out ImmutableArray<RewardTrackTaskRef> refs)
            ? refs
            : [];

    public async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext db = await dbContextFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            List<RewardTrackEntity> trackRows = await db
                .RewardTracks.AsNoTracking()
                .Where(t => t.DeletedAt == null)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // Four queries for the whole catalogue rather than four per track: a hotel running ten
            // campaigns would otherwise open forty round trips every time an operator saved a row.
            List<RewardTrackTaskEntity> taskRows = await db
                .RewardTrackTasks.AsNoTracking()
                .Where(t => t.DeletedAt == null)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            List<RewardTrackTaskLevelEntity> levelRows = await db
                .RewardTrackTaskLevels.AsNoTracking()
                .Where(l => l.DeletedAt == null)
                .OrderBy(l => l.LevelIndex)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            List<RewardTrackTaskConditionEntity> conditionRows = await db
                .RewardTrackTaskConditions.AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            List<RewardTrackPrizeEntity> prizeRows = await db
                .RewardTrackPrizes.AsNoTracking()
                .Where(p => p.DeletedAt == null)
                .OrderBy(p => p.RequiredPoints)
                .ThenBy(p => p.SortOrder)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            List<RewardTrackPrizeRewardEntity> rewardRows = await db
                .RewardTrackPrizeRewards.AsNoTracking()
                .Where(r => r.DeletedAt == null)
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ILookup<int, RewardTrackTaskEntity> tasksByTrack = taskRows.ToLookup(t =>
                t.RewardTrackEntityId
            );
            ILookup<int, RewardTrackTaskLevelEntity> levelsByTask = levelRows.ToLookup(l =>
                l.RewardTrackTaskEntityId
            );
            ILookup<int, RewardTrackTaskConditionEntity> conditionsByTask = conditionRows.ToLookup(
                c => c.RewardTrackTaskEntityId
            );
            ILookup<int, RewardTrackPrizeEntity> prizesByTrack = prizeRows.ToLookup(p =>
                p.RewardTrackEntityId
            );
            ILookup<int, RewardTrackPrizeRewardEntity> rewardsByPrize = rewardRows.ToLookup(r =>
                r.RewardTrackPrizeEntityId
            );

            ImmutableArray<RewardTrackDefinitionSnapshot> tracks =
            [
                .. trackRows.Select(t =>
                    BuildTrack(
                        t,
                        tasksByTrack[t.Id],
                        levelsByTask,
                        conditionsByTask,
                        prizesByTrack[t.Id],
                        rewardsByPrize
                    )
                ),
            ];

            _tracks = tracks;
            _index = Index.Build(tracks);

            logger.LogInformation(
                "Loaded {TrackCount} reward track(s); {ActionCount} action code(s) have at least one task.",
                tracks.Length,
                _index.Actions.Count
            );
        }
        catch (Exception ex)
        {
            // The previous catalogue stays. Every player keeps the tracks they were already being
            // served rather than every campaign vanishing because one query timed out.
            logger.LogError(
                ex,
                "Failed to load the reward-track catalog; keeping the previous one."
            );
        }
    }

    private static RewardTrackDefinitionSnapshot BuildTrack(
        RewardTrackEntity track,
        IEnumerable<RewardTrackTaskEntity> tasks,
        ILookup<int, RewardTrackTaskLevelEntity> levelsByTask,
        ILookup<int, RewardTrackTaskConditionEntity> conditionsByTask,
        IEnumerable<RewardTrackPrizeEntity> prizes,
        ILookup<int, RewardTrackPrizeRewardEntity> rewardsByPrize
    ) =>
        new()
        {
            TrackId = track.TrackId,
            Theme = track.Theme,
            Status = track.Status,
            SortOrder = track.SortOrder,
            StartsAtUtc = track.StartsAt,
            ProgressEndsAtUtc = track.ProgressEndsAt,
            ClaimEndsAtUtc = track.ClaimEndsAt,
            UnlockKind = track.UnlockKind,
            UnlockValue = track.UnlockValue,
            CompletionPolicy = track.CompletionPolicy,
            ContentVersion = track.ContentVersion,
            Hidden = track.Hidden,
            CampaignCode = track.CampaignCode,
            Premium = track.PremiumEnabled
                ? new RewardTrackPremiumSnapshot
                {
                    BoostPerMille = track.PremiumBoostPerMille,
                    InstantPoints = track.PremiumInstantPoints,
                    CostCredits = track.PremiumCostCredits,
                    CostDiamonds = track.PremiumCostDiamonds,
                }
                : null,
            Tasks =
            [
                .. tasks.Select(t => BuildTask(t, levelsByTask[t.Id], conditionsByTask[t.Id])),
            ],
            Prizes = [.. prizes.Select(p => BuildPrize(p, rewardsByPrize[p.Id]))],
        };

    private static RewardTrackTaskDefinitionSnapshot BuildTask(
        RewardTrackTaskEntity task,
        IEnumerable<RewardTrackTaskLevelEntity> levels,
        IEnumerable<RewardTrackTaskConditionEntity> conditions
    ) =>
        new()
        {
            TaskId = task.TaskId,
            ActionCode = task.ActionCode,
            Parameter = task.Parameter,
            Mode = task.Mode,
            Premium = task.Premium,
            SortOrder = task.SortOrder,
            // Order is presentation only -- they are ANDed -- so the operator's own order is kept.
            Conditions =
            [
                .. conditions
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new RewardTrackTaskConditionSnapshot
                    {
                        Field = c.Field,
                        Operator = c.Operator,
                        Value = c.Value,
                    }),
            ],
            // Ordered by requirement, not by the stored index: the stage math walks them in
            // ascending order and a content edit that renumbers badly would otherwise pay stages
            // out of sequence.
            Levels =
            [
                .. levels
                    .OrderBy(l => l.RequiredCount)
                    .Select(
                        (l, i) =>
                            new RewardTrackTaskLevelSnapshot
                            {
                                LevelIndex = i,
                                RequiredCount = l.RequiredCount,
                                PointsReward = l.PointsReward,
                                Premium = l.Premium,
                            }
                    ),
            ],
        };

    private static RewardTrackPrizeDefinitionSnapshot BuildPrize(
        RewardTrackPrizeEntity prize,
        IEnumerable<RewardTrackPrizeRewardEntity> rewards
    ) =>
        new()
        {
            PrizeId = prize.PrizeId,
            RequiredPoints = prize.RequiredPoints,
            Premium = prize.Premium,
            SortOrder = prize.SortOrder,
            Rewards =
            [
                .. rewards.Select(r => new RewardGrantSnapshot
                {
                    Kind = r.Kind,
                    RewardTypeId = r.RewardTypeId,
                    Amount = r.Amount,
                    ExtraParams = r.ExtraParams,
                }),
            ],
        };

    /// <summary>
    /// The lookups, rebuilt as one unit so a reload is never observed half-applied: the field is
    /// swapped once, and a reader sees all of the old catalogue or all of the new one.
    /// </summary>
    private sealed record Index(
        IReadOnlyDictionary<string, RewardTrackDefinitionSnapshot> ByTrackId,
        IReadOnlySet<string> Actions,
        IReadOnlyDictionary<string, ImmutableArray<RewardTrackTaskRef>> TasksByAction
    )
    {
        public static Index Empty { get; } =
            new(
                new Dictionary<string, RewardTrackDefinitionSnapshot>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                new Dictionary<string, ImmutableArray<RewardTrackTaskRef>>(StringComparer.Ordinal)
            );

        public static Index Build(ImmutableArray<RewardTrackDefinitionSnapshot> tracks)
        {
            Dictionary<string, RewardTrackDefinitionSnapshot> byTrackId = new(
                StringComparer.Ordinal
            );
            Dictionary<string, List<RewardTrackTaskRef>> byAction = new(StringComparer.Ordinal);

            foreach (RewardTrackDefinitionSnapshot track in tracks)
            {
                byTrackId[track.TrackId] = track;

                // Archived tracks are indexed too. They accept no progress -- the grain checks the
                // clock and the status -- but leaving them out of the index would make the answer
                // to "which tasks care about this?" depend on a schedule, and the index is content.
                foreach (RewardTrackTaskDefinitionSnapshot task in track.Tasks)
                {
                    if (!byAction.TryGetValue(task.ActionCode, out List<RewardTrackTaskRef>? list))
                    {
                        list = [];
                        byAction[task.ActionCode] = list;
                    }

                    list.Add(new RewardTrackTaskRef(track.TrackId, task.TaskId));
                }
            }

            Dictionary<string, ImmutableArray<RewardTrackTaskRef>> frozen = new(
                StringComparer.Ordinal
            );

            foreach ((string action, List<RewardTrackTaskRef> refs) in byAction)
            {
                frozen[action] = [.. refs];
            }

            return new Index(
                byTrackId,
                new HashSet<string>(byAction.Keys, StringComparer.Ordinal),
                frozen
            );
        }
    }
}
