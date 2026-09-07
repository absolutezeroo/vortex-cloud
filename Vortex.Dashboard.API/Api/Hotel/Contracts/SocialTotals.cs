namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The social graph as a whole, except <paramref name="WindowMessages"/> which is the window.
/// </summary>
/// <param name="Friendships">Friendships, counted once.</param>
/// <param name="FriendRows">The stored rows, which are two per friendship because both directions
/// are written on accept. Kept beside the halved figure so the two never look like a discrepancy.
/// </param>
public sealed record SocialTotals(
    int Friendships,
    int FriendRows,
    int PlayersWithFriends,
    int PendingRequests,
    int BlockedPairs,
    int IgnoredPairs,
    int TotalMessages,
    int Undelivered,
    int WindowMessages,
    int Threads,
    int Posts
);
