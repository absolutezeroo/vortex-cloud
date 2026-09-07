namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One entry of the collector leaderboard. The name is null for a deleted player.</summary>
public sealed record CollectorScore(int PlayerId, string? PlayerName, int Score);
