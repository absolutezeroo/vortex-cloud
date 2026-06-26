using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetLevelUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required int PetId { get; init; }

    [Id(1)]
    public required int Level { get; init; }
}
