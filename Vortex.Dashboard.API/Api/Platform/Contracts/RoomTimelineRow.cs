using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One thing that happened in a room.
/// </summary>
/// <param name="EventType">Which journal the row came from: <c>entry</c>, <c>chat</c> or
/// <c>item</c>. The three are read into one shape so the page can sort them into a single column of
/// time, which is the only order an investigation reads them in.</param>
/// <param name="Message">The line said, for a chat; the event payload, for an item move; null for
/// an entry.</param>
public sealed record RoomTimelineRow(
    DateTime CreatedAt,
    string EventType,
    int? PlayerId,
    string? PlayerName,
    string? Message,
    int? TargetPlayerId,
    string? TargetPlayerName,
    long? ItemId
);
