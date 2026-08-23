using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Room.Furniture;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record FloorHeightMapMessageComposer : IComposer
{
    [Id(0)]
    public required RoomScaleType ScaleType { get; init; }

    [Id(1)]
    public required int FixedWallsHeight { get; init; }

    [Id(2)]
    public required string ModelData { get; init; }

    [Id(3)]
    public required List<AreaHideDataSnapshot> AreaHideData { get; init; }

    [Id(4)]
    public int CameraInitX { get; init; }

    [Id(5)]
    public int CameraInitY { get; init; }

    [Id(6)]
    public float CameraInitZ { get; init; }
}
