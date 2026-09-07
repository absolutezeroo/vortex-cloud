namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>How many players sit at one level.</summary>
public sealed record AchievementLevelCount(int Level, int Players);
