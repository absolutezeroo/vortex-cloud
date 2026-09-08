using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One guild in the roster.</summary>
/// <param name="OwnerName">Null when the owner's account is gone, which the row still has to show.</param>
public sealed record GuildDirectoryRow(
    int Id,
    string Name,
    string? BadgeUrl,
    string Type,
    int OwnerId,
    string? OwnerName,
    int RoomId,
    string? RoomName,
    int Members,
    int PendingRequests,
    int Bans,
    int Threads,
    DateTime CreatedAt
);
