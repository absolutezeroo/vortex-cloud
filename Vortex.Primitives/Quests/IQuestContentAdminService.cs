using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Quests.Admin;

namespace Vortex.Primitives.Quests;

/// <summary>
/// CRUD for the content behind the quest system that is not a quest: community goals with their
/// ladder, and daily-task definitions with their rewards. Community-goal writes reload the
/// <see cref="Grains.ICommunityGoalGrain"/> so the live goal never drifts from the database.
/// </summary>
public interface IQuestContentAdminService
{
    Task<QuestContentAdminResult> CreateCommunityGoalAsync(
        CommunityGoalSpec spec,
        CancellationToken ct
    );

    Task<QuestContentAdminResult> UpdateCommunityGoalAsync(
        int goalId,
        CommunityGoalSpec spec,
        CancellationToken ct
    );

    Task<QuestContentAdminResult> DeleteCommunityGoalAsync(int goalId, CancellationToken ct);

    Task<QuestContentAdminResult> CreateDailyTaskAsync(DailyTaskSpec spec, CancellationToken ct);

    Task<QuestContentAdminResult> UpdateDailyTaskAsync(
        int taskId,
        DailyTaskSpec spec,
        CancellationToken ct
    );

    Task<QuestContentAdminResult> DeleteDailyTaskAsync(int taskId, CancellationToken ct);
}
