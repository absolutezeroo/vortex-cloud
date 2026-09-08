using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One member of a guild.</summary>
/// <param name="IsOwner">
/// The owner is in the list and cannot be removed from it — shown rather than filtered out, because
/// an operator looking for who is responsible for a guild is looking for exactly this row.
/// </param>
public sealed record GuildMemberRow(
    int PlayerId,
    string? PlayerName,
    string Rank,
    bool IsOwner,
    DateTime JoinedAt
);
