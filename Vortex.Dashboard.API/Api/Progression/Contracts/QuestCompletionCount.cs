namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One quest and how often it was finished.
/// </summary>
/// <param name="Name">campaign.localization, falling back to the number for a quest since deleted.</param>
public sealed record QuestCompletionCount(int QuestId, string Name, int Completions);
