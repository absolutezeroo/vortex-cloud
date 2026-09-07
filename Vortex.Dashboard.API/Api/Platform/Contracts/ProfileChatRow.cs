using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One line the account said.</summary>
public sealed record ProfileChatRow(
    DateTime CreatedAt,
    int RoomId,
    string? RoomName,
    string Message
);
