using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Quests;
using Vortex.Primitives.Quests;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read surface for the content behind the quest system that is not a quest: community goals with
/// their ladder and standing, and daily-task definitions with their rewards and take-up. Authoring
/// lives in <c>DashboardOperationsService.QuestContent.cs</c>.
/// </summary>
internal sealed partial class DashboardApiService
{
    /// <summary>
    /// Every community goal with its ladder and where the hotel currently stands on it. The active
    /// one is flagged: exactly one goal is served to players, and which one is a rule (enabled,
    /// in-window, lowest sort order) an operator should not have to reconstruct by eye.
    /// </summary>
    public Task<object> CommunityGoalsAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                List<CommunityGoalEntity> goals = await db
                    .CommunityGoals.AsNoTracking()
                    .OrderBy(g => g.SortOrder)
                    .ThenBy(g => g.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                if (goals.Count == 0)
                {
                    return new { count = 0, items = Array.Empty<object>() };
                }

                List<int> goalIds = [.. goals.Select(g => g.Id)];

                ILookup<int, CommunityGoalLevelEntity> levelsByGoal = (
                    await db
                        .CommunityGoalLevels.AsNoTracking()
                        .Where(l => goalIds.Contains(l.CommunityGoalEntityId))
                        .OrderBy(l => l.ScoreThreshold)
                        .ThenBy(l => l.LevelNumber)
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).ToLookup(l => l.CommunityGoalEntityId);

                Dictionary<int, (int Total, int Contributors)> standings = (
                    await db
                        .PlayerCommunityGoalContributions.AsNoTracking()
                        .Where(c => goalIds.Contains(c.CommunityGoalEntityId))
                        .GroupBy(c => c.CommunityGoalEntityId)
                        .Select(g => new
                        {
                            GoalId = g.Key,
                            Total = g.Sum(c => c.Score),
                            Contributors = g.Count(),
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).ToDictionary(x => x.GoalId, x => (x.Total, x.Contributors));

                DateTime now = DateTime.UtcNow;

                // The same rule the grain applies, so the page cannot disagree with what players see.
                int? activeGoalId = goals
                    .Where(g => g.Enabled && (g.EndsAt is null || g.EndsAt > now))
                    .Select(g => (int?)g.Id)
                    .FirstOrDefault();

                var items = goals
                    .Select(goal =>
                    {
                        (int total, int contributors) = standings.GetValueOrDefault(goal.Id);
                        List<CommunityGoalLevelEntity> levels = [.. levelsByGoal[goal.Id]];

                        return new
                        {
                            goal.Id,
                            goal.Code,
                            goal.CampaignCode,
                            goal.ScorePerQuest,
                            goal.Enabled,
                            goal.EndsAt,
                            goal.SortOrder,
                            expired = goal.EndsAt is { } endsAt && endsAt <= now,
                            isActive = activeGoalId == goal.Id,
                            totalScore = total,
                            contributors,
                            reachedLevel = levels.Count(l => total >= l.ScoreThreshold),
                            levels = levels
                                .Select(l => new
                                {
                                    l.Id,
                                    l.LevelNumber,
                                    l.ScoreThreshold,
                                    l.RewardUserLimit,
                                    reached = total >= l.ScoreThreshold,
                                })
                                .ToList(),
                        };
                    })
                    .ToList();

                return new { count = items.Count, items };
            },
            ct
        );

    /// <summary>
    /// Daily-task definitions with their rewards and how they have actually landed: how many
    /// assignments each has produced, and how many were finished and claimed. A task nobody ever
    /// completes is the one worth re-tuning.
    /// </summary>
    public Task<object> DailyTasksAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                List<DailyTaskEntity> tasks = await db
                    .DailyTasks.AsNoTracking()
                    .OrderBy(t => t.IsBonus)
                    .ThenBy(t => t.SortOrder)
                    .ThenBy(t => t.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                if (tasks.Count == 0)
                {
                    return new
                    {
                        count = 0,
                        items = Array.Empty<object>(),
                        questTypes = QuestTypeNames(),
                    };
                }

                List<int> taskIds = [.. tasks.Select(t => t.Id)];

                ILookup<int, DailyTaskRewardEntity> rewardsByTask = (
                    await db
                        .DailyTaskRewards.AsNoTracking()
                        .Where(r => taskIds.Contains(r.DailyTaskEntityId))
                        .OrderBy(r => r.Id)
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).ToLookup(r => r.DailyTaskEntityId);

                Dictionary<int, (int Assigned, int Completed, int Claimed)> stats = (
                    await db
                        .PlayerDailyTasks.AsNoTracking()
                        .Where(a => taskIds.Contains(a.DailyTaskEntityId))
                        .GroupBy(a => a.DailyTaskEntityId)
                        .Select(g => new
                        {
                            TaskId = g.Key,
                            Assigned = g.Count(),
                            Completed = g.Count(a => a.Status != DailyTaskStatus.Available),
                            Claimed = g.Count(a => a.Status == DailyTaskStatus.Claimed),
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).ToDictionary(x => x.TaskId, x => (x.Assigned, x.Completed, x.Claimed));

                var items = tasks
                    .Select(task =>
                    {
                        (int assigned, int completed, int claimed) = stats.GetValueOrDefault(
                            task.Id
                        );

                        return new
                        {
                            task.Id,
                            task.TaskCode,
                            task.QuestTypeCode,
                            task.IsBonus,
                            task.ImageVersion,
                            task.CatalogName,
                            task.RequiredRepeats,
                            task.Enabled,
                            task.SortOrder,
                            assigned,
                            completed,
                            claimed,
                            completionRate = assigned <= 0
                                ? 0
                                : Math.Round(completed * 100d / assigned, 1),
                            rewards = rewardsByTask[task.Id]
                                .Select(r => new
                                {
                                    r.Id,
                                    r.ProductItemTypeId,
                                    r.RewardTypeId,
                                    r.ExtraParams,
                                    r.Amount,
                                })
                                .ToList(),
                        };
                    })
                    .ToList();

                return new
                {
                    count = items.Count,
                    items,
                    questTypes = QuestTypeNames(),
                };
            },
            ct
        );

    /// <summary>
    /// The objective vocabulary daily tasks share with quests, read by reflection so the picker can
    /// never offer a type the progression code does not know.
    /// </summary>
    private static List<string> QuestTypeNames() =>
        [
            .. typeof(QuestTypes)
                .GetFields(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                )
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .Where(name => !name.StartsWith("offer_", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal),
        ];
}
