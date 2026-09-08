using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One thread and every post in it, hidden and deleted ones included.</summary>
/// <remarks>
/// The whole point of opening a thread from here is to read what was removed, so nothing is filtered
/// out: a post the guild hid still carries its text, its author and who hid it.
/// </remarks>
public sealed record GuildThreadDetail(
    int GuildId,
    string GuildName,
    GuildThreadRow Thread,
    IReadOnlyList<GuildPostRow> Posts
);
