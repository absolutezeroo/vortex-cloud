namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>Who holds the most bots. The name is null for a deleted player.</summary>
public sealed record BotOwnerCount(int OwnerId, string? OwnerName, int BotCount);
