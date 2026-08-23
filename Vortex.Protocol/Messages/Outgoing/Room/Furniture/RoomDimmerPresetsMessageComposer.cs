using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>
/// Everything the moodlight dialog draws: the three stored presets, which one is selected, and
/// whether the lamp is currently on.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomDimmerPresetsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<RoomDimmerPresetSnapshot> Presets { get; init; }

    [Id(1)]
    public required int SelectedPresetId { get; init; }

    [Id(2)]
    public required bool IsOn { get; init; }

    [Id(3)]
    public required RoomObjectId ItemId { get; init; }
}
