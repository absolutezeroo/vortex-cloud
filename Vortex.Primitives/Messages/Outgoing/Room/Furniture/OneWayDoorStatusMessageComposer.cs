using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Furniture;

[GenerateSerializer, Immutable]
public sealed record OneWayDoorStatusMessageComposer : IComposer
{
    [Id(0)]
    public required int FurniId { get; init; }

    [Id(1)]
    public required int Status { get; init; }
}
