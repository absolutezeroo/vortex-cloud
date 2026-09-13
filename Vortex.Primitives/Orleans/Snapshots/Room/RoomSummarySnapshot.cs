using System;
using Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Orleans.Snapshots.Room;

[GenerateSerializer, Immutable]
public record RoomSummarySnapshot
{
    [Id(0)]
    public required RoomId RoomId { get; init; } = -1;

    [Id(1)]
    public required string Name { get; init; } = string.Empty;

    [Id(2)]
    public required string Description { get; init; } = string.Empty;

    [Id(3)]
    public required PlayerId OwnerId { get; init; } = -1;

    [Id(4)]
    public required string OwnerName { get; init; } = string.Empty;

    [Id(5)]
    public required int Population { get; init; } = 0;

    [Id(6)]
    public required DateTime LastUpdatedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this room is handling a raid right now.
    /// </summary>
    /// <remarks>
    /// Not <c>required</c>, unlike everything above it: this is a late addition and false is the
    /// honest answer for every caller that has no idea — a room reporting its own summary knows,
    /// the directory knows because the room told it, and nobody else should have to say.
    /// </remarks>
    [Id(7)]
    public bool RaidIncidentActive { get; init; }
}
