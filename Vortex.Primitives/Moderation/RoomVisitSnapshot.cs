using Orleans;

namespace Vortex.Primitives.Moderation;

/// <summary>
/// One line of the mod tool's "room visits" list. The client renders the entry time from the hour
/// and minute alone — it has no date field — so the list is only meaningful for a recent window.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomVisitSnapshot
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required string RoomName { get; init; }

    [Id(2)]
    public required int EnterHour { get; init; }

    [Id(3)]
    public required int EnterMinute { get; init; }
}
