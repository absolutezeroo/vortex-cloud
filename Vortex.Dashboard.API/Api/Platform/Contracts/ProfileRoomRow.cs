using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One room the account owns.</summary>
public sealed record ProfileRoomRow(
    int RoomId,
    string RoomName,
    int UsersNow,
    int PlayersMax,
    DateTime LastActive,
    string Model
);
