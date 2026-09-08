using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One forum post, as written.</summary>
/// <param name="State">
/// <c>Visible</c>, <c>Hidden</c> (a guild moderator) or <c>HiddenByAdmin</c> (an operator). The
/// three read the same to a player — the post is gone — and differently to whoever is deciding what
/// to do about it.
/// </param>
/// <param name="Deleted">Kept in the answer rather than filtered out: what was deleted is usually
/// the thing being asked about.</param>
public sealed record GuildPostRow(
    int Id,
    int ThreadId,
    string Message,
    string State,
    bool Deleted,
    int AuthorId,
    string? AuthorName,
    int? AdminId,
    string? AdminName,
    DateTime CreatedAt,
    DateTime? AdminOperationAt
);
