namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>Who holds the most friends. The name is null for a deleted player.</summary>
public sealed record SocialFriendedCount(int PlayerId, string? PlayerName, int Friends);
