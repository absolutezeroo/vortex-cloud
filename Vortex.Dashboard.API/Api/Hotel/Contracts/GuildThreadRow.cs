using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One forum thread of a guild, hidden ones included.</summary>
/// <remarks>
/// Deliberately shows what the players cannot see. A thread hidden by a guild admin is the one an
/// operator is most often asked about — "they hid it, look at what it said" — so the state is a
/// column here rather than a filter.
/// </remarks>
public sealed record GuildThreadRow(
    int Id,
    string Subject,
    string State,
    bool IsPinned,
    int PostCount,
    int AuthorId,
    string? AuthorName,
    DateTime CreatedAt,
    DateTime? LastPostAt
);
