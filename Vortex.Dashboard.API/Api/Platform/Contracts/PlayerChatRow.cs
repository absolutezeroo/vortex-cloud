using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One line this account said, and to whom when it was a whisper.</summary>
public sealed record PlayerChatRow(
    DateTime CreatedAt,
    int RoomId,
    string? RoomName,
    string Message,
    int? TargetPlayerId,
    string? TargetPlayerName
);
