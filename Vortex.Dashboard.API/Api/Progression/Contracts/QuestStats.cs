using System.Collections.Generic;
using Vortex.Dashboard.API.Api.Hotel.Contracts;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>Quest completions over the window, and which quests they were.</summary>
public sealed record QuestStats(
    ReportWindow Window,
    QuestTotals Totals,
    IReadOnlyList<QuestCompletionPoint> Timeline,
    IReadOnlyList<QuestCompletionCount> TopQuests
);
