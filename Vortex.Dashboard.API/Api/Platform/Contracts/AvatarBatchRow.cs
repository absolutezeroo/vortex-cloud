namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One player's head. The url is null for a player with no figure.</summary>
public sealed record AvatarBatchRow(int Id, string? AvatarUrl);
