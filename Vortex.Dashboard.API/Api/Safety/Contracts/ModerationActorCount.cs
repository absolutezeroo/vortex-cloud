namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>Which moderators act the most. The name is null for a deleted player.</summary>
public sealed record ModerationActorCount(long ActorPlayerId, string? ActorName, int Count);
