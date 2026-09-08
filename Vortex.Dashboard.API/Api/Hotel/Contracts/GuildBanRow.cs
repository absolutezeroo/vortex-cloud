using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One player barred from rejoining a guild.</summary>
/// <param name="BlockedByName">
/// Who barred them. A guild ban has no reason field on the row, so the name is the only context an
/// operator gets before deciding whether to lift it.
/// </param>
public sealed record GuildBanRow(
    int PlayerId,
    string? PlayerName,
    int BlockedById,
    string? BlockedByName,
    DateTime BlockedAt
);
