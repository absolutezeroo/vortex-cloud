namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One rung of the angling ladder.</summary>
public sealed record FishingLevelRow(int Id, int Level, int XpThreshold);
