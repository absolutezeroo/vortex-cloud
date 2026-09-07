using System;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One line of chat.
/// </summary>
/// <param name="TargetPlayerId">Null for room-wide chat; set when the line was addressed to someone.</param>
/// <remarks>The three names are null when their row has since been deleted.</remarks>
public sealed record ChatlogEntry(
    int Id,
    DateTime CreatedAt,
    int RoomId,
    string? RoomName,
    int PlayerId,
    string? PlayerName,
    int? TargetPlayerId,
    string? TargetPlayerName,
    string Message
);
