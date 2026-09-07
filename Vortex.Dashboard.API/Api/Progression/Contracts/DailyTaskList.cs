using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The daily tasks and how they have landed.
/// </summary>
/// <param name="QuestTypes">The objective vocabulary tasks share with quests, read by reflection so
/// the picker can never offer a type the progression code does not know.</param>
public sealed record DailyTaskList(
    int Count,
    IReadOnlyList<DailyTaskRow> Items,
    IReadOnlyList<string> QuestTypes
);
