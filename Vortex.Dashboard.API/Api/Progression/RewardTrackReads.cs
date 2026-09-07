using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.RewardTracks;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.Signals;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read surface for reward-track content and player progression. The CRUD lives in
/// <see cref="Operations.RewardOperations"/>; here we only read.
/// </summary>
internal sealed class RewardTrackReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    ISignalVocabulary signalVocabulary
) : DashboardReads(dbContextFactory)
{
    private readonly ISignalVocabulary _signalVocabulary = signalVocabulary;

    /// <summary>
    /// The action codes a task can be defined on, and — the part that matters — whether anything
    /// actually raises each one today.
    /// </summary>
    /// <remarks>
    /// A task on a code with no producer never advances, and a bar that never moves is the worst
    /// thing this content system can ship. The flag is computed from the handlers that exist in
    /// <c>RewardTrackEventHandlers</c>, so it stops being a lie the moment one is added.
    /// </remarks>
    public object RewardTrackActionOptions()
    {
        // The action list is declared, not derived from the translators that exist: two actions
        // have no producer today and content may already name them, so deriving would make a task
        // written on one disappear from the editor instead of being flagged inert.
        List<object> items =
        [
            .. SignalActions
                .All.OrderBy(name => name, StringComparer.Ordinal)
                .Select(name =>
                    (object)
                        new
                        {
                            name,
                            // Whether anything actually raises this action. Read from the loaded
                            // translators, so it is a fact rather than a hand-kept list.
                            wired = _signalVocabulary.ShapesFor(name).Length > 0,
                            // What a step on this action can filter on, and what a later step can
                            // point back at, typed so the editor knows which control to draw. The
                            // shapes come from the translators themselves: a fact the action never
                            // emits cannot be offered here.
                            facts = FactsOf(name),
                        }
                ),
        ];

        return new { count = items.Count, items };
    }

    /// <summary>
    /// Every fact a step on this action can be filtered on, typed.
    /// </summary>
    /// <remarks>
    /// The kind is what lets one filter editor serve every action: it decides whether the operator
    /// gets a picker, a select of declared values or a text box, and which operators are offered.
    /// Before it, that mapping was hardcoded per page, which is why only this page had pickers.
    /// <para>
    /// <c>target</c> is included when the action has one, and typed by the action rather than
    /// globally: the target of <c>create_room</c> is a room and the target of <c>give_respect</c> is
    /// a player. It is a real fact -- the signal host republishes it -- it is simply not declared by
    /// a translator among its facts.
    /// </para>
    /// </remarks>
    private object[] FactsOf(string action)
    {
        Dictionary<string, object> byKey = new(StringComparer.Ordinal);

        foreach (SignalShape shape in _signalVocabulary.ShapesFor(action))
        {
            if (shape.TargetKind is FactKind targetKind)
            {
                byKey[Facts.TargetKey] = Describe(
                    new FactKey(Facts.TargetKey, targetKind, "rewardTracks.fact_target", "Target")
                );
            }

            foreach (FactKey fact in shape.Facts)
            {
                byKey[fact.Key] = Describe(fact);
            }
        }

        return [.. byKey.Values];
    }

    private static object Describe(FactKey fact) =>
        new
        {
            key = fact.Key,
            kind = fact.Kind.ToString(),
            labelKey = fact.LabelKey,
            fallbackLabel = fact.FallbackLabel,
            // Which operators mean anything here. Sent rather than inferred client-side: the
            // validator refuses the others, and an editor that offered them would be inviting a
            // save it knows will fail.
            operators = FactOperators.For(fact.Kind),
            values = fact.EnumValues.IsDefaultOrEmpty
                ? []
                : fact
                    .EnumValues.Select(v =>
                        (object)
                            new
                            {
                                value = v.Value,
                                labelKey = v.LabelKey,
                                fallbackLabel = v.FallbackLabel,
                            }
                    )
                    .ToArray(),
        };

    /// <summary>The reward kinds, with the client's own product-type id and what the target field means.</summary>
    public object RewardTrackRewardKindOptions()
    {
        List<object> items =
        [
            .. Enum.GetValues<RewardKind>()
                .Select(k =>
                    (object)
                        new
                        {
                            name = k.ToString(),
                            value = (int)k,
                            target = RewardTargetHint(k),
                        }
                ),
        ];

        return new { count = items.Count, items };
    }

    /// <summary>Every track, its content, and how the hotel is doing on it.</summary>
    public Task<object> RewardTracksAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                string search = (query["search"] ?? string.Empty).Trim();

                List<RewardTrackEntity> tracks = await db
                    .RewardTracks.AsNoTracking()
                    .Where(t => t.DeletedAt == null)
                    .OrderBy(t => t.SortOrder)
                    .ThenBy(t => t.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                if (search.Length > 0)
                {
                    tracks =
                    [
                        .. tracks.Where(t =>
                            t.TrackId.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || t.CampaignCode.Contains(search, StringComparison.OrdinalIgnoreCase)
                        ),
                    ];
                }

                // Five queries for the whole list rather than five per track.
                List<RewardTrackTaskEntity> tasks = await db
                    .RewardTrackTasks.AsNoTracking()
                    .Where(t => t.DeletedAt == null)
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<RewardTrackTaskLevelEntity> levels = await db
                    .RewardTrackTaskLevels.AsNoTracking()
                    .Where(l => l.DeletedAt == null)
                    .OrderBy(l => l.LevelIndex)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<RewardTrackTaskStepEntity> steps = await db
                    .RewardTrackTaskSteps.AsNoTracking()
                    .Where(s => s.DeletedAt == null)
                    .OrderBy(s => s.StepIndex)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<RewardTrackStepFilterEntity> stepFilters = await db
                    .RewardTrackStepFilters.AsNoTracking()
                    .Where(f => f.DeletedAt == null)
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<RewardTrackPrizeEntity> prizes = await db
                    .RewardTrackPrizes.AsNoTracking()
                    .Where(p => p.DeletedAt == null)
                    .OrderBy(p => p.RequiredPoints)
                    .ThenBy(p => p.SortOrder)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<RewardTrackPrizeRewardEntity> rewards = await db
                    .RewardTrackPrizeRewards.AsNoTracking()
                    .Where(r => r.DeletedAt == null)
                    .OrderBy(r => r.SortOrder)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<string, (int Participants, int Completions, int Premium)> stats =
                    await db
                        .PlayerRewardTracks.Where(t => t.DeletedAt == null)
                        .GroupBy(t => t.TrackId)
                        .Select(g => new
                        {
                            TrackId = g.Key,
                            Participants = g.Count(),
                            Completions = g.Count(x => x.CompletedAt != null),
                            Premium = g.Count(x => x.PremiumUnlocked),
                        })
                        .ToDictionaryAsync(
                            x => x.TrackId,
                            x => (x.Participants, x.Completions, x.Premium),
                            ct
                        )
                        .ConfigureAwait(false);

                Dictionary<string, int> claims = await db
                    .PlayerRewardTrackClaims.Where(c => c.DeletedAt == null)
                    .GroupBy(c => c.TrackId)
                    .Select(g => new { TrackId = g.Key, Claims = g.Count() })
                    .ToDictionaryAsync(x => x.TrackId, x => x.Claims, ct)
                    .ConfigureAwait(false);

                List<object> items = [];

                foreach (RewardTrackEntity track in tracks)
                {
                    List<RewardTrackTaskEntity> trackTasks =
                    [
                        .. tasks.Where(t => t.RewardTrackEntityId == track.Id),
                    ];
                    List<RewardTrackPrizeEntity> trackPrizes =
                    [
                        .. prizes.Where(p => p.RewardTrackEntityId == track.Id),
                    ];

                    (int participants, int completions, int premium) = stats.GetValueOrDefault(
                        track.TrackId
                    );

                    // The ceilings the content validator measures milestones against, shown here so
                    // an operator can see why a prize is out of reach without publishing to find out.
                    int freePoints = 0;
                    int premiumPoints = track.PremiumEnabled ? track.PremiumInstantPoints : 0;

                    foreach (RewardTrackTaskEntity task in trackTasks)
                    {
                        foreach (
                            RewardTrackTaskLevelEntity level in levels.Where(l =>
                                l.RewardTrackTaskEntityId == task.Id
                            )
                        )
                        {
                            premiumPoints += level.PointsReward;

                            if (!task.Premium && !level.Premium)
                            {
                                freePoints += level.PointsReward;
                            }
                        }
                    }

                    items.Add(
                        new
                        {
                            id = track.Id,
                            trackId = track.TrackId,
                            localizationKey = $"reward_track.{track.TrackId}.name",
                            theme = track.Theme,
                            status = track.Status.ToString(),
                            sortOrder = track.SortOrder,
                            startsAt = track.StartsAt,
                            progressEndsAt = track.ProgressEndsAt,
                            claimEndsAt = track.ClaimEndsAt,
                            unlockKind = track.UnlockKind.ToString(),
                            unlockValue = track.UnlockValue,
                            completionPolicy = track.CompletionPolicy.ToString(),
                            premiumEnabled = track.PremiumEnabled,
                            premiumBoostPerMille = track.PremiumBoostPerMille,
                            premiumInstantPoints = track.PremiumInstantPoints,
                            premiumCostCredits = track.PremiumCostCredits,
                            premiumCostDiamonds = track.PremiumCostDiamonds,
                            contentVersion = track.ContentVersion,
                            hidden = track.Hidden,
                            campaignCode = track.CampaignCode,
                            freePointCeiling = freePoints,
                            premiumPointCeiling = premiumPoints,
                            participants,
                            completions,
                            premiumHolders = premium,
                            prizesClaimed = claims.GetValueOrDefault(track.TrackId),
                            tasks = trackTasks
                                .Select(t => new
                                {
                                    id = t.Id,
                                    taskId = t.TaskId,
                                    localizationKey = $"reward_track.{track.TrackId}.task.{t.TaskId}.name",
                                    actionCode = t.ActionCode,
                                    wired = WiredRewardTrackActions.Contains(t.ActionCode),
                                    parameter = t.Parameter,
                                    mode = t.Mode.ToString(),
                                    premium = t.Premium,
                                    sortOrder = t.SortOrder,
                                    // Empty for a plain task: the engine builds its single step from
                                    // the action above, and the editor pre-fills the same way.
                                    steps = steps
                                        .Where(s => s.RewardTrackTaskEntityId == t.Id)
                                        .Select(s => new
                                        {
                                            stepIndex = s.StepIndex,
                                            actionCode = s.ActionCode,
                                            filters = stepFilters
                                                .Where(f => f.RewardTrackTaskStepEntityId == s.Id)
                                                .Select(f => new
                                                {
                                                    factKey = f.FactKey,
                                                    op = (int)f.Operator,
                                                    value = f.Value,
                                                })
                                                .ToList(),
                                        })
                                        .ToList(),
                                    levels = levels
                                        .Where(l => l.RewardTrackTaskEntityId == t.Id)
                                        .Select(l => new
                                        {
                                            levelIndex = l.LevelIndex,
                                            requiredCount = l.RequiredCount,
                                            pointsReward = l.PointsReward,
                                            premium = l.Premium,
                                        })
                                        .ToList(),
                                })
                                .ToList(),
                            prizes = trackPrizes
                                .Select(p => new
                                {
                                    id = p.Id,
                                    prizeId = p.PrizeId,
                                    requiredPoints = p.RequiredPoints,
                                    premium = p.Premium,
                                    sortOrder = p.SortOrder,
                                    reachable = p.RequiredPoints
                                        <= (p.Premium ? premiumPoints : freePoints),
                                    rewards = rewards
                                        .Where(r => r.RewardTrackPrizeEntityId == p.Id)
                                        .Select(r => new
                                        {
                                            id = r.Id,
                                            kind = r.Kind.ToString(),
                                            kindValue = (int)r.Kind,
                                            rewardTypeId = r.RewardTypeId,
                                            amount = r.Amount,
                                            extraParams = r.ExtraParams,
                                            sortOrder = r.SortOrder,
                                        })
                                        .ToList(),
                                })
                                .ToList(),
                        }
                    );
                }

                return new { count = items.Count, items };
            },
            ct
        );

    /// <summary>One player's standing on every track they have touched, plus their task progress.</summary>
    public Task<object> PlayerRewardTracksAsync(int playerId, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                List<PlayerRewardTrackEntity> rows = await db
                    .PlayerRewardTracks.AsNoTracking()
                    .Where(t => t.PlayerEntityId == playerId && t.DeletedAt == null)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<PlayerRewardTrackTaskEntity> tasks = await db
                    .PlayerRewardTrackTasks.AsNoTracking()
                    .Where(t => t.PlayerEntityId == playerId && t.DeletedAt == null)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<PlayerRewardTrackClaimEntity> claims = await db
                    .PlayerRewardTrackClaims.AsNoTracking()
                    .Where(c => c.PlayerEntityId == playerId && c.DeletedAt == null)
                    .OrderByDescending(c => c.ClaimedAt)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    playerId,
                    count = rows.Count,
                    items = rows.Select(r => new
                        {
                            trackId = r.TrackId,
                            points = r.Points,
                            premiumUnlocked = r.PremiumUnlocked,
                            premiumUnlockedAt = r.PremiumUnlockedAt,
                            completedAt = r.CompletedAt,
                            contentVersion = r.ContentVersion,
                            tasks = tasks
                                .Where(t => t.TrackId == r.TrackId)
                                .Select(t => new
                                {
                                    taskId = t.TaskId,
                                    progressCount = t.ProgressCount,
                                    highestPaidLevelIndex = t.HighestPaidLevelIndex,
                                })
                                .ToList(),
                            claims = claims
                                .Where(c => c.TrackId == r.TrackId)
                                .Select(c => new
                                {
                                    prizeId = c.PrizeId,
                                    claimedAt = c.ClaimedAt,
                                    pointsAtClaim = c.PointsAtClaim,
                                    // What was actually handed over, rendered at claim time. The
                                    // prize definition can be rewritten afterwards; this cannot,
                                    // which is what makes "why does this player have that?"
                                    // answerable a year later.
                                    granted = c.GrantedSummary,
                                })
                                .ToList(),
                        })
                        .ToList(),
                };
            },
            ct
        );

    /// <summary>
    /// The action codes something actually raises today, from the handlers in
    /// <c>Vortex.RewardTracks/Events/RewardTrackEventHandlers.cs</c>. Kept here rather than
    /// discovered by reflection because this project does not reference that assembly — the
    /// duplication is deliberate and small, and the walkthrough says to update both together.
    /// </summary>
    private static readonly HashSet<string> WiredRewardTrackActions = new(StringComparer.Ordinal)
    {
        RewardTrackActions.EnterOtherUsersRoom,
        RewardTrackActions.ChatWithSomeone,
        RewardTrackActions.Dance,
        RewardTrackActions.Wave,
        RewardTrackActions.RequestFriend,
        RewardTrackActions.GiveRespect,
        RewardTrackActions.ChangeFigure,
        RewardTrackActions.ChangeMotto,
        RewardTrackActions.WearBadge,
        RewardTrackActions.CreateRoom,
        RewardTrackActions.PlaceItem,
        RewardTrackActions.MoveItem,
        RewardTrackActions.RotateItem,
        RewardTrackActions.PickUpItem,
        RewardTrackActions.WalkOnFurni,
        RewardTrackActions.PetLevel,
        RewardTrackActions.BuyFromCatalogue,
        RewardTrackActions.SpendCredits,
        RewardTrackActions.CompleteTrade,
        RewardTrackActions.SendMessengerMessage,
        RewardTrackActions.UseHabbicon,
        RewardTrackActions.CompleteHabbiconCollection,
        RewardTrackActions.CompleteQuest,
        RewardTrackActions.AchievementLevel,
    };

    private static string RewardTargetHint(RewardKind kind) =>
        kind switch
        {
            RewardKind.WallItem => "wall item type id",
            RewardKind.FloorItem => "furniture definition id",
            RewardKind.AvatarEffect => "effect id",
            RewardKind.Badge => "badge code",
            RewardKind.Bot => "bot name (extra params = figure)",
            RewardKind.Currency => "activity point type: -1 credits, 0 duckets, 5 diamonds",
            RewardKind.ChatStyle => "chat style id",
            RewardKind.Pet => "pet type (extra params = figure)",
            RewardKind.Habbicon => "habbicon id",
            RewardKind.Entitlement => "perk code, e.g. TRADE",
            _ => string.Empty,
        };
}
