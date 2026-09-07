using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One room in the picker.
/// </summary>
/// <param name="OwnerName">Null for a room whose owner has been deleted.</param>
/// <param name="UsersNow">Who is in it at this moment, which is what the browsing order ranks by.</param>
public sealed record RoomDirectoryRow(
    int Id,
    string Name,
    string? OwnerName,
    int UsersNow,
    int PlayersMax,
    DateTime LastActive
);
