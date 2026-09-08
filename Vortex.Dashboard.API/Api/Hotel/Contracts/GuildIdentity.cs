using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>Who the guild is, and where it lives.</summary>
/// <param name="ForumEnabled">
/// False when the guild has no forum settings row at all, which is not the same as an empty forum:
/// the first is a guild whose forum was never opened, the second one nobody has posted in.
/// </param>
public sealed record GuildIdentity(
    int Id,
    string Name,
    string? Description,
    string? BadgeUrl,
    string Type,
    int OwnerId,
    string? OwnerName,
    int RoomId,
    string? RoomName,
    bool ForumEnabled,
    int MemberCount,
    DateTime CreatedAt
);
