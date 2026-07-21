using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Quests.Admin;

namespace Vortex.Primitives.Quests;

/// <summary>
/// CRUD for the <c>quests</c> table, used by the dashboard's quest admin surface. Every write reloads
/// the <see cref="Grains.IQuestManagerGrain"/> definition cache so the live quests players see never
/// drift from the database — see the implementation.
/// </summary>
public interface IQuestAdminService
{
    Task<QuestAdminResult> CreateAsync(QuestCreateSpec spec, CancellationToken ct);
    Task<QuestAdminResult> UpdateAsync(int questId, QuestUpdateSpec spec, CancellationToken ct);
    Task<QuestAdminResult> DeleteAsync(int questId, CancellationToken ct);
}
