using System;
using Orleans;

namespace Vortex.Primitives.Habbicons.Snapshots;

/// <summary>One row of a player's Habbicon ownership.</summary>
[GenerateSerializer, Immutable]
public sealed record PlayerHabbiconSnapshot
{
    [Id(0)]
    public required int HabbiconId { get; init; }

    [Id(1)]
    public required HabbiconState State { get; init; }

    [Id(2)]
    public required DateTime AcquiredAtUtc { get; init; }

    [Id(3)]
    public required HabbiconSource Source { get; init; }
}
