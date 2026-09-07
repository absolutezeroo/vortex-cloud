namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>Who gets reported. The name is null for a player that no longer exists.</summary>
public sealed record CfhReportedPlayer(int PlayerId, string? PlayerName, int ReportCount);
