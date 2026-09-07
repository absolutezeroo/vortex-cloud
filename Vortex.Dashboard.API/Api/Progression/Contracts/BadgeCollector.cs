namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>Who holds the most badges. The name is null for a deleted player.</summary>
public sealed record BadgeCollector(int PlayerId, string? PlayerName, int Badges);
