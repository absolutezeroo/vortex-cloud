using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Snapshots.Furniture;

/// <summary>
/// A moodlight as its dialog needs to see it: the three presets, which one is selected, and whether
/// the lamp is on.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomDimmerStateSnapshot
{
    [Id(0)]
    public required RoomObjectId ItemId { get; init; }

    [Id(1)]
    public required ImmutableArray<RoomDimmerPresetSnapshot> Presets { get; init; }

    [Id(2)]
    public required int SelectedPresetId { get; init; }

    [Id(3)]
    public required bool IsOn { get; init; }
}
