using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// Everything an operator needs to act on one guild, in one request.
/// </summary>
/// <remarks>
/// The four lists are the four things that get acted on, and they are answered together on purpose:
/// the reason someone opens a guild is a report, and a report names a post whose author is a member
/// whose request someone approved. Splitting them into four calls would mean four tabs to answer
/// one question.
/// </remarks>
public sealed record GuildModeration(
    GuildIdentity Guild,
    IReadOnlyList<GuildMemberRow> Members,
    IReadOnlyList<GuildRequestRow> PendingRequests,
    IReadOnlyList<GuildBanRow> Bans,
    IReadOnlyList<GuildThreadRow> Threads
);
