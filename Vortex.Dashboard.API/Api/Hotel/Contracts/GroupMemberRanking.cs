namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// A guild in the members leaderboard.
/// </summary>
/// <param name="Badge">The badge code as stored; <paramref name="BadgeUrl"/> is where to draw it
/// from, resolved here because the badge renderer is not translatable to SQL. The URL is null when
/// the hotel has no badge renderer configured, which is a deployment that shows the code instead.
/// </param>
public sealed record GroupMemberRanking(
    int GroupId,
    string Name,
    string Badge,
    string? BadgeUrl,
    int OwnerId,
    string OwnerName,
    int MemberCount,
    int RoomId
);
