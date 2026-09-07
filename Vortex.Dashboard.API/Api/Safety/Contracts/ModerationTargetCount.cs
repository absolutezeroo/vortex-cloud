namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>Who is sanctioned the most. The name is null for a deleted player.</summary>
public sealed record ModerationTargetCount(long TargetPlayerId, string? TargetName, int Count);
