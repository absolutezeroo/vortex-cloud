namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The window's completions, against two all-time figures.
/// </summary>
/// <param name="TotalAccepted">Every quest ever accepted, not the window: it is the denominator a
/// completion count means something against.</param>
/// <param name="ActivePlayers">Players holding a quest they have not finished, right now.</param>
public sealed record QuestTotals(int TotalCompletions, int TotalAccepted, int ActivePlayers);
