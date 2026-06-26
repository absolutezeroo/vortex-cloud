using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetVocalMessageComposer : IComposer
{
    [Id(0)]
    public required RoomObjectId PetObjectId { get; init; }

    [Id(1)]
    public required int PetType { get; init; }

    [Id(2)]
    public required string VocalType { get; init; }

    [Id(3)]
    public required int VocalIndex { get; init; }
}
